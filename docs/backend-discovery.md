# Backend Discovery — PersonalBudget API

> **Projeto:** PersonalBudget.Api (.NET 9)
> **Stack:** ASP.NET Core 9 · Clean Architecture · EF Core · PostgreSQL · JWT · Fly.io
> **Data:** Setembro 2026
> **Relacionado:** [Frontend Discovery](../../PersonalBudgetUI/docs/frontend-discovery.md) · [Product Vision](product-vision.md)

---

## Executive Summary

O backend é uma API REST bem estruturada com Clean Architecture, DDD, domain-rich model e boas práticas de separação de responsabilidades. A arquitetura é sólida e escalável. Os maiores riscos atualmente são **críticos de segurança** (hashing de senha inseguro, segredos expostos) e gaps de **qualidade/manutenção** que crescem à medida que o projeto evolui.

### Top 5 Prioridades

| # | Item | Severidade |
|---|------|-----------|
| 1 | 🔴 SHA-256 para senhas (deve virar BCrypt/Argon2id) | CRÍTICO |
| 2 | 🔴 JWT secret exposto em appsettings.json | CRÍTICO |
| 3 | 🔴 CORS AllowAnyOrigin em produção | Alta |
| 4 | 🔴 Sem rate limiting em endpoints de autenticação | Alta |
| 5 | 🟡 TransactionService com responsabilidade excessiva | Média |

---

## 1. Segurança

### 1.1 Hashing de Senha com SHA-256 🔴 CRÍTICO

**Localização:** `PersonalBudget.Infrastructure/Security/PasswordHasher.cs`

**Problema:** SHA-256 é um algoritmo de hash de propósito geral, extremamente rápido — GPUs modernas conseguem bilhões de hashes por segundo. Isso torna ataques de dicionário e brute-force viáveis em segundos.

**Solução:** Migrar para `BCrypt.Net-Next` ou `Argon2id` (via `Isopoh.Cryptography.Argon2`):

```csharp
// Antes (inseguro)
public string Hash(string password) =>
    Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

// Depois (BCrypt — work factor ajustável)
public string Hash(string password) =>
    BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

public bool Verify(string password, string hash) =>
    BCrypt.Net.BCrypt.Verify(password, hash);
```

**Plano de migração:** Hash novo na próxima autenticação (lazy migration) — ao fazer login com senha correta, re-hashar com BCrypt e salvar o novo hash.

| Esforço | Impacto | Risco de Não Fazer |
|---------|---------|-------------------|
| Baixo | Crítico | Todos os usuários vulneráveis |

---

### 1.2 JWT Secret em appsettings.json 🔴 CRÍTICO

**Localização:** `PersonalBudget.Api/appsettings.json` e/ou `appsettings.Development.json`

**Problema:** O segredo JWT em texto plano no repositório permite que qualquer pessoa com acesso ao código gere tokens válidos para qualquer usuário.

**Solução:**

**Desenvolvimento:** .NET User Secrets
```bash
dotnet user-secrets set "Jwt:Secret" "sua-chave-super-secreta-minimo-32-chars"
```

**Produção (Fly.io):**
```bash
flyctl secrets set JWT_SECRET="sua-chave-super-secreta-minimo-32-chars"
```

```csharp
// Program.cs — ler do ambiente
builder.Configuration.AddEnvironmentVariables();
var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET not configured");
```

| Esforço | Impacto | Risco de Não Fazer |
|---------|---------|-------------------|
| Muito Baixo | Crítico | Autenticação comprometida |

---

### 1.3 CORS AllowAnyOrigin 🔴 Alta

**Localização:** `PersonalBudget.Api/Program.cs` (configuração de CORS)

**Problema:** `AllowAnyOrigin()` permite que qualquer website na internet faça requisições autenticadas à API em nome do usuário logado.

**Solução:**

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

```json
// appsettings.json
{
  "Cors": {
    "AllowedOrigins": ["https://personalbudget.fly.dev", "http://localhost:5173"]
  }
}
```

| Esforço | Impacto | Risco de Não Fazer |
|---------|---------|-------------------|
| Muito Baixo | Alto | CSRF / dados expostos |

---

### 1.4 Sem Rate Limiting em Autenticação 🔴 Alta

**Problema:** Os endpoints `/api/authentication/login` e `/api/authentication/signin` não têm limitação de tentativas. Um atacante pode tentar senhas ilimitadamente.

**Solução:** ASP.NET Core 8+ tem rate limiting nativo:

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 10;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 0;
    });
});

// No controller de autenticação
[HttpPost("login")]
[RateLimiter(policyName: "auth")]
public async Task<IActionResult> Login(...)
```

| Esforço | Impacto | Risco de Não Fazer |
|---------|---------|-------------------|
| Baixo | Alto | Brute-force de contas |

---

### 1.5 Sem Refresh Token 🟡 Média

**Problema:** O token JWT expira em 60 minutos, forçando o usuário a fazer login novamente. Não há mecanismo de refresh.

**Solução:** Implementar refresh token com rotação:
1. No login, retornar `{ accessToken, refreshToken }` — refresh token armazenado no banco com expiração de 30 dias
2. Endpoint `POST /api/authentication/refresh` — valida o refresh token, emite novo access + refresh
3. Invalidar o refresh token antigo a cada rotação (rotation)

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Médio | Médio | Baixo |

---

## 2. Qualidade e Manutenibilidade

### 2.1 TransactionService com Responsabilidade Excessiva 🟡 Média

**Localização:** `PersonalBudget.Application/Services/TransactionService.cs`

**Problema:** O serviço combina criação (3 estratégias), listagem, atualização, exclusão em lote, atualização de status e importação CSV — todas em um único serviço. Isso torna o arquivo difícil de testar e manter.

**Solução:** Introduzir **MediatR** para CQRS com pipeline behaviors:

```csharp
// Commands
public record CreateTransactionCommand(Guid UserId, CreateTransactionRequest Request) : IRequest<TransactionDto>;
public record UpdateTransactionCommand(Guid TransactionId, UpdateTransactionRequest Request) : IRequest<Unit>;
public record DeleteTransactionCommand(Guid TransactionId) : IRequest<DeleteResult>;

// Handlers
public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    // lógica focada em criação apenas
}
```

**Benefício:** Pipeline behaviors para logging, validação e caching transversais:

```csharp
// Logging automático de todos os commands
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Alto | Médio | Médio |

---

### 2.2 Sem Unit of Work 🟡 Média

**Problema:** Operações que afetam múltiplos repositórios (ex: criar uma transferência cria 2 transações + debita 1 conta + credita outra) não são atômicas. Se o segundo save falhar, os dados ficam em estado inconsistente.

**Solução:** O `AppDbContext` do EF Core já é uma Unit of Work. O padrão é garantir que operações multi-entidade usem a mesma instância e façam `SaveChangesAsync()` uma única vez ao final:

```csharp
// Antes (arriscado — múltiplos SaveChanges)
await _accountRepo.SaveAsync(debitedAccount);
await _accountRepo.SaveAsync(creditedAccount);
await _transactionRepo.SaveAsync(transaction1);
await _transactionRepo.SaveAsync(transaction2);

// Depois (atômico)
_context.Accounts.Update(debitedAccount);
_context.Accounts.Update(creditedAccount);
_context.Transactions.AddRange(transaction1, transaction2);
await _context.SaveChangesAsync(); // uma única transação de banco
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Médio | Alto | Médio |

---

### 2.3 Sem Validação de DTOs 🟡 Média

**Problema:** Os requests/commands chegam sem validação explícita. Ex: `CreateTransactionCommand` com `Amount = -500` ou `Date = DateTime.MinValue` chegam ao domain sem barreira.

**Solução:** **FluentValidation** com integração automática no pipeline:

```csharp
// Validator
public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Request.Amount).GreaterThan(0).WithMessage("Valor deve ser positivo");
        RuleFor(x => x.Request.Date).NotEmpty().LessThanOrEqualTo(DateTime.Today.AddDays(30));
        RuleFor(x => x.Request.Description).NotEmpty().MaximumLength(200);
    }
}

// Program.cs — registro automático
builder.Services.AddValidatorsFromAssembly(typeof(CreateTransactionCommand).Assembly);
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Médio | Médio | Baixo |

---

### 2.4 Sem Soft Delete e Auditoria 🟡 Média

**Problema:** Deletar uma transação é permanente. Não há histórico de quem criou/alterou cada registro. Para um app financeiro, perda de dados e falta de auditoria são riscos sérios.

**Solução:**

```csharp
// Interface de auditoria
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid CreatedByUserId { get; set; }
}

// Interface de soft delete
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

// Global query filter no AppDbContext
modelBuilder.Entity<Transaction>().HasQueryFilter(t => !t.IsDeleted);

// SaveChangesAsync override para auto-preencher campos de auditoria
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Médio | Alto | Baixo |

---

### 2.5 Risco de N+1 Queries 🟡 Média

**Problema:** Alguns repositórios carregam entidades sem `Include()` e depois acessam propriedades de navegação em loop, causando N+1 queries ao banco.

**Diagnóstico:** Habilitar logging de SQL do EF Core para identificar queries excessivas:

```csharp
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**Solução:** Usar `Include()` explícito nas queries de leitura e considerar projeção com `.Select()` para dados que não precisam de rastreamento.

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Médio | Médio | Baixo |

---

### 2.6 Sem Structured Logging 🟡 Média

**Problema:** Sem injeção de `ILogger`, diagnóstico de problemas em produção (Fly.io) depende inteiramente de exceções não capturadas. Operações críticas (login, criação de transação, pagamento de fatura) não deixam rastro.

**Solução:** Serilog com sink para console (Fly.io captura stdout) + Seq/Datadog opcional:

```csharp
// Program.cs
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console(new JsonFormatter()));

// Em services
public class TransactionService(ILogger<TransactionService> logger, ...)
{
    public async Task CreateAsync(CreateTransactionCommand cmd)
    {
        logger.LogInformation("Creating transaction {Type} {Amount} for user {UserId}",
            cmd.Type, cmd.Amount, cmd.UserId);
        // ...
    }
}
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Baixo | Alto | Baixo |

---

### 2.7 Paginação Inconsistente 🟢 Baixa

**Problema:** Transações e faturas têm paginação implementada, mas Categorias, Contas e Membros carregam tudo sem limite. Para households com muitas categorias, isso pode ser problemático.

**Solução:** Padronizar um `PagedResult<T>` e aplicar em todos os endpoints de listagem:

```csharp
public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public bool HasNextPage => Page * PageSize < Total;
}
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Médio | Baixo | Baixo |

---

## 3. Observabilidade

### 3.1 Sem Health Check Endpoint 🟡 Média

**Problema:** Fly.io e orquestradores precisam de `/health` para saber se a instância está pronta. Sem isso, a plataforma não sabe distinguir uma instância lenta de uma morta.

**Solução:** ASP.NET Core tem suporte nativo:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres")
    .AddDbContextCheck<AppDbContext>(name: "ef-core");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions {
    Predicate = check => check.Tags.Contains("ready")
});
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Muito Baixo | Médio | Baixo |

---

### 3.2 Swagger Incompleto 🟢 Baixa

**Problema:** Controllers não têm `[ProducesResponseType]` para todos os status codes possíveis. O Swagger gerado não documenta respostas de erro (400, 401, 404, 422), dificultando a integração do frontend.

**Solução:**

```csharp
[HttpPost]
[ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
public async Task<IActionResult> CreateTransaction(...)
```

**Extra:** Configurar ProblemDetails globalmente para respostas de erro padronizadas (RFC 7807).

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Baixo | Baixo | Baixo |

---

### 3.3 Sem OpenTelemetry / APM 🟢 Baixa

**Problema:** Não há rastreamento distribuído nem métricas de performance. Impossível identificar queries lentas ou gargalos sem logs manuais.

**Solução:** OpenTelemetry com exportador para Fly.io / Sentry:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter());
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Baixo | Médio | Baixo |

---

## 4. Testes

### 4.1 Cobertura Limitada — Falta Camada Application 🟡 Média

**Situação atual:**
- `PersonalBudget.Domain.Tests`: testes unitários de entidades e value objects ✅
- `PersonalBudget.Integration.Tests`: testes de integração com Testcontainers ✅
- **Faltando:** testes unitários dos Application Services

**O que adicionar:**

```
PersonalBudget.Application.Tests/
├── Services/
│   ├── TransactionServiceTests.cs
│   ├── AccountServiceTests.cs
│   └── CreditCardStatementServiceTests.cs
└── Strategies/
    ├── AccountTransactionStrategyTests.cs
    └── TransferStrategyTests.cs
```

Usar mocks dos repositórios para isolar a lógica de aplicação do banco.

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Médio | Alto | Baixo |

---

### 4.2 Sem Testes de Contrato 🟢 Baixa

**Problema:** O arquivo `FRONTEND_API_CHANGES.txt` no repositório evidencia que quebras de contrato entre backend e frontend já ocorreram.

**Solução:** Consumer-driven contract testing com **Pact.NET**:
- Frontend define contratos de API esperados
- Backend roda os contratos em CI para verificar compatibilidade
- Quebras de contrato falham o build antes de chegar em produção

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Alto | Alto | Baixo |

---

## 5. Arquitetura / Escalabilidade

### 5.1 Cache para Dados de Baixa Mutação 🟡 Média

**Problema:** Categorias e perfis de membros são carregados em toda requisição, mesmo que raramente mudem.

**Solução:** `IMemoryCache` com invalidação na mutação:

```csharp
public async Task<IEnumerable<CategoryDto>> GetAllAsync(Guid householdId)
{
    var cacheKey = $"categories:{householdId}";
    if (_cache.TryGetValue(cacheKey, out IEnumerable<CategoryDto> cached))
        return cached;

    var categories = await _repository.GetByHouseholdAsync(householdId);
    _cache.Set(cacheKey, categories, TimeSpan.FromMinutes(10));
    return categories;
}

// Invalidar no create/update/delete
public async Task CreateAsync(CreateCategoryCommand cmd)
{
    // ...
    _cache.Remove($"categories:{cmd.HouseholdId}");
}
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Baixo | Médio | Baixo |

---

### 5.2 Background Job para Extensão de Recorrências 🟡 Média

**Problema:** Transações fixas (recorrentes) são materializadas para 12 meses na criação. Em Setembro/2027, as primeiras recorrências criadas vão expirar silenciosamente. O usuário precisa lembrar de estender manualmente.

**Solução:** Background job que roda mensalmente e estende recorrências ativas antes de expirar:

```csharp
// Usando .NET Worker Service
public class RecurrenceExtensionWorker(IServiceProvider services) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRecurrenceExtensionService>();
            await service.ExtendExpiringRecurrencesAsync();

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Médio | Alto | Baixo |

---

### 5.3 Notificações de Vencimento de Fatura 🟢 Baixa

**Problema:** Não há mecanismo para avisar o usuário quando a fatura do cartão está próxima de vencer.

**Solução (fases):**
1. **Fase 1:** Endpoint `GET /api/dashboard/alerts` que retorna alertas calculados on-demand
2. **Fase 2:** Email via SendGrid quando `DueDate - hoje <= 3 dias`
3. **Fase 3:** Push notification via Web Push API

| Esforço | Impacto | Risco |
|---------|---------|-------|
| Médio | Alto | Baixo |

---

## 6. Tabela Consolidada de Prioridades

| Item | Severidade | Impacto | Esforço | Quick Win? |
|------|-----------|---------|---------|------------|
| BCrypt para senhas | 🔴 CRÍTICO | Crítico | Baixo | ✅ Sim |
| JWT secret em variável de ambiente | 🔴 CRÍTICO | Crítico | Muito Baixo | ✅ Sim |
| CORS restrito | 🔴 Alta | Alto | Muito Baixo | ✅ Sim |
| Rate limiting em auth | 🔴 Alta | Alto | Baixo | ✅ Sim |
| Structured logging (Serilog) | 🟡 Média | Alto | Baixo | ✅ Sim |
| Health check endpoint | 🟡 Média | Médio | Muito Baixo | ✅ Sim |
| Unit of Work atômico | 🟡 Média | Alto | Médio | Não |
| Soft delete + auditoria | 🟡 Média | Alto | Médio | Não |
| FluentValidation em DTOs | 🟡 Média | Médio | Médio | Não |
| Application layer tests | 🟡 Média | Alto | Médio | Não |
| Cache de categorias/perfis | 🟡 Média | Médio | Baixo | ✅ Sim |
| Background job de recorrências | 🟡 Média | Alto | Médio | Não |
| Refresh token | 🟡 Média | Médio | Médio | Não |
| MediatR / CQRS | 🟢 Baixa | Médio | Alto | Não |
| OpenTelemetry | 🟢 Baixa | Médio | Baixo | Não |
| Testes de contrato (Pact) | 🟢 Baixa | Alto | Alto | Não |

---

*Documento gerado em Setembro 2026. Para roadmap de produto completo, ver [product-vision.md](product-vision.md).*
