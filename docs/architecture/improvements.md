# PersonalBudget — Recomendacoes de Melhoria

Analise da arquitetura atual identificou os pontos abaixo, organizados por **prioridade** (Alta / Media / Baixa) e **categoria**.

---

## 1. Seguranca (Alta Prioridade)

### 1.1 Algoritmo de Hash de Senha Inseguro

**Problema:** `PasswordHasher` usa SHA-256 simples com Base64. SHA-256 e um algoritmo rapido, ideal para hashing de dados, mas pessimo para senhas — um atacante com GPU moderna pode testar bilhoes de candidatos por segundo.

**Arquivo:** `PersonalBudget.Infrastructure/Security/PasswordHasher.cs`

**Solucao recomendada:**
```csharp
// Substituir por BCrypt.Net-Next ou Microsoft.AspNetCore.Identity.PasswordHasher
using BCrypt.Net;

public string Hash(string password) => BCrypt.HashPassword(password, workFactor: 12);
public bool Verify(string password, string hash) => BCrypt.Verify(password, hash);
```

---

### 1.2 Segredo JWT em Texto Claro no appsettings

**Problema:** `appsettings.json` contem a chave JWT em texto claro. Em producao, isso expoe o segredo no repositorio ou em logs.

**Arquivo:** `PersonalBudget.Api/appsettings.json` / `JwtSettings.cs`

**Solucao recomendada:**
- Usar `dotnet user-secrets` em desenvolvimento.
- Em producao: variaveis de ambiente (`JWT__Key`) ou Azure Key Vault / AWS Secrets Manager.
- Validar que a chave tem no minimo 256 bits (32 chars).

---

### 1.3 CORS AllowAll em Producao

**Problema:** `CorsExtensions.cs` configura `AllowAnyOrigin()` para todos os ambientes, incluindo producao.

**Solucao recomendada:**
```csharp
// Restringir origins em producao via configuracao
builder.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>()!)
       .AllowAnyMethod()
       .AllowAnyHeader();
```

---

### 1.4 Ausencia de Rate Limiting

**Problema:** Nenhum rate limiting nos endpoints de autenticacao (`/signin`, `/login`), expondo o sistema a ataques de brute-force e credential stuffing.

**Solucao recomendada:**
```csharp
// Program.cs — .NET 7+ built-in rate limiting
builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("auth", o => {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
    });
});
// Aplicar [EnableRateLimiting("auth")] no AuthenticationController
```

---

### 1.5 Ausencia de Refresh Token

**Problema:** Tokens JWT com expiracao de 60 minutos sem mecanismo de refresh forcam o usuario a re-autenticar frequentemente ou incentivam expiracao longa (pior).

**Solucao recomendada:** Implementar endpoint `POST /api/authentication/refresh` com refresh token de longa duracao (armazenado em banco), revogavel por sessao.

---

## 2. Persistencia e Dados (Alta Prioridade)

### 2.1 Banco In-Memory Nao e Adequado para Producao

**Problema:** `DatabaseExtensions.cs` configura `UseInMemoryDatabase`. Dados sao perdidos ao reiniciar a aplicacao. Comportamentos do In-Memory divergem do SQL real (sem transacoes reais, sem constraints FK).

**Arquivo:** `PersonalBudget.Api/Extensions/DatabaseExtensions.cs`

**Solucao recomendada:**
```csharp
// Desenvolvimento: SQLite com arquivo
options.UseSqlite("Data Source=personalbudget_dev.db");

// Producao: PostgreSQL (compativel com Fly.io)
options.UseNpgsql(connectionString);
```
Adicionar migrations EF Core (`dotnet ef migrations add InitialCreate`).

---

### 2.2 Ausencia de Migrations EF Core

**Problema:** Sem migrations, nao ha controle de versao do schema. Atualizacoes de schema em producao exigem `EnsureCreated()` (que e destrutivo) ou SQL manual.

**Solucao recomendada:**
```bash
dotnet ef migrations add InitialCreate --project PersonalBudget.Infrastructure --startup-project PersonalBudget.Api
dotnet ef database update
```
Aplicar migrations automaticamente no startup apenas em dev/test. Em producao, aplicar via pipeline CI/CD.

---

### 2.3 Falta de Soft Delete

**Problema:** Exclusoes sao permanentes (`repository.Remove(entity)`). Isso impossibilita auditoria e recuperacao de dados.

**Solucao recomendada:** Adicionar `IsDeleted: bool` e `DeletedAt: DateTime?` em entidades criticas (`Transaction`, `Account`, `CreditCard`). Usar EF Core Global Query Filter:
```csharp
modelBuilder.Entity<Transaction>().HasQueryFilter(t => !t.IsDeleted);
```

---

### 2.4 Potencial Problema N+1 em Queries

**Problema:** Varios repositorios carregam entidades sem Include explicito, podendo causar N+1 queries ao acessar propriedades de navegacao em loops.

**Locais de atencao:**
- `TransactionQueryRepository` — verificar uso de `.Include()` em queries de listagem
- `CreditCardRepository` — statements sao carregados como navigation property `_statements`

**Solucao recomendada:**
```csharp
// Usar Include/ThenInclude onde necessario
dbContext.CreditCards
    .Include(c => c._statements.Where(s => s.Status == BillStatus.Open))
    .Where(c => c.HouseholdId == householdId)
    .ToListAsync();

// Para queries de leitura pesadas, usar projecoes com Select (evita carregar entidade completa)
dbContext.Transactions
    .Where(t => t.HouseholdId == householdId)
    .Select(t => new TransactionDto { ... })
    .ToListAsync();
```

---

### 2.5 SaveChangesAsync Duplicado por Repositorio

**Problema:** Cada repositorio chama `SaveChangesAsync()` individualmente. Isso impede operacoes atomicas que envolvem multiplos repositorios (ex: TransferTransactionCreationStrategy grava 2 transacoes e 2 accounts — se uma falhar a meio, o estado fica inconsistente).

**Solucao recomendada:** Implementar o padrao Unit of Work explicitamente:
```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// AppDbContext implementa IUnitOfWork
// Services chamam _unitOfWork.SaveChangesAsync() ao final da operacao
```

---

## 3. Qualidade de Codigo e Manutencao (Media Prioridade)

### 3.1 Ausencia de Logging Estruturado

**Problema:** Nenhum `ILogger<T>` injetado nos services. Sem logs, e impossivel diagnosticar erros em producao ou auditar operacoes criticas (criacao de transacao, fechamento de fatura, mudanca de status).

**Solucao recomendada:**
```csharp
// Injetar ILogger nos services criticos
private readonly ILogger<TransactionService> _logger;

_logger.LogInformation("Transaction {TransactionId} created for household {HouseholdId}", 
    transaction.Id, command.HouseholdId);
```
Configurar Serilog com sink estruturado (Seq, Elastic, ou simplesmente JSON para stdout no Fly.io).

---

### 3.2 Ausencia de Trilha de Auditoria

**Problema:** Nao ha registro de quem alterou o que e quando em entidades financeiras. Fundamental para um sistema de financas pessoais.

**Solucao recomendada:** Interceptor EF Core para audit trail automatico:
```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    // Registra entradas Added/Modified/Deleted com UserId, timestamp e valores anteriores
}
```
Ou adicionar campos `CreatedBy`, `UpdatedAt`, `UpdatedBy` nas entidades principais.

---

### 3.3 Cobertura de Testes Insuficiente

**Problema:** Apenas `PersonalBudget.Domain.Tests` existe, sem testes para Application Services, Repositories ou Controllers. Estrategias de criacao de transacao, que sao o core do negocio, nao tem cobertura observavel.

**Solucao recomendada:**
- **Testes de unidade:** Application Services com repositorios mockados (use `NSubstitute` ou `Moq`).
- **Testes de integracao:** Usar `WebApplicationFactory<Program>` + SQLite In-Memory para testar endpoints end-to-end.
- **Testes de dominio:** Cobrir todas as transicoes de estado (`Transaction.Complete()`, `Transaction.Cancel()`, regras de `CreditCardStatement`).

---

### 3.4 Falta de Validacao de Entrada nos DTOs

**Problema:** Commands e Requests nao possuem atributos de validacao (`[Required]`, `[Range]`, `[MaxLength]`). Erros de dominio so sao lancados dentro do dominio, sem feedback antecipado ao cliente.

**Solucao recomendada:** Adicionar `FluentValidation` ou Data Annotations:
```csharp
public class CreateTransactionCommand
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount deve ser positivo")]
    public decimal Amount { get; set; }

    [Required]
    public string Description { get; set; } = default!;
}
```
Registrar `app.UseRequestValidation()` via middleware ou `[ApiController]` com `ModelState` automatico.

---

### 3.5 Responsabilidade Excessiva em TransactionService

**Problema:** `TransactionService` gerencia criacao, consulta, atualizacao de status, exclusao simples, exclusao em lote e delegacao para strategies — muitas responsabilidades em uma unica classe.

**Solucao recomendada:** Separar em handlers distintos seguindo CQRS informal:
- `CreateTransactionHandler`
- `UpdateTransactionStatusHandler`
- `DeleteTransactionHandler`
- `GetTransactionsQueryHandler`

Ou adotar `MediatR` para formalizar comandos e queries.

---

## 4. Performance e Escalabilidade (Media Prioridade)

### 4.1 Ausencia de Paginacao Consistente

**Problema:** Alguns endpoints retornam listas sem paginacao (ex: `GET /api/categories`, `GET /api/accounts`). Em households com muitas categorias/contas, isso causa queries pesadas.

**Solucao recomendada:** Padronizar paginacao via query params (`?page=1&pageSize=20`) ou cursor-based pagination para transacoes.

---

### 4.2 Ausencia de Cache

**Problema:** Dados de baixa mutacao (categorias do sistema, perfis do household) sao relidos do banco a cada requisicao.

**Solucao recomendada:** Cache em memoria com `IMemoryCache` para categorias e perfis de household, com invalidacao ao criar/atualizar:
```csharp
_cache.GetOrCreateAsync($"household-profiles-{householdId}", entry => {
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return _profileRepo.GetByHouseholdAsync(householdId);
});
```

---

### 4.3 Ausencia de Health Checks

**Problema:** Sem endpoint de health check, o Fly.io nao consegue detectar falhas de inicializacao ou degradacao do servico automaticamente.

**Solucao recomendada:**
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

app.MapHealthChecks("/health");
```

---

## 5. Observabilidade e Operacao (Baixa Prioridade)

### 5.1 Ausencia de OpenTelemetry / Distributed Tracing

**Recomendacao:** Adicionar `OpenTelemetry` para traces e metricas, especialmente util ao depurar fluxos multi-step como criacao de transferencia ou importacao CSV.

### 5.2 Documentacao Swagger Incompleta

**Problema:** Poucos endpoints tem `[ProducesResponseType]` para todos os codigos de status possiveis (404, 403, 422).

**Recomendacao:** Completar anotacoes Swagger e adicionar exemplos de request/response com `[SwaggerRequestExample]` (Swashbuckle.AspNetCore.Filters).

### 5.3 DevUser — Codigo de Desenvolvimento em Producao

**Problema:** `DevUser.cs` sugere atalhos de autenticacao para dev. Garantir que nao e ativado por profile incorreto em producao.

**Recomendacao:** Proteger com `if (app.Environment.IsDevelopment())` e adicionar teste que falhe se `DevUser` for acessivel em outros ambientes.

---

## Resumo Executivo

| # | Problema | Prioridade | Esforco |
|---|---|---|---|
| 1.1 | Hash de senha SHA-256 | Alta | Baixo |
| 1.2 | JWT secret em appsettings | Alta | Baixo |
| 1.3 | CORS AllowAll em producao | Alta | Baixo |
| 1.4 | Rate limiting em autenticacao | Alta | Medio |
| 2.1 | Banco In-Memory em producao | Alta | Alto |
| 2.2 | Migrations EF Core ausentes | Alta | Medio |
| 2.5 | Falta de Unit of Work atomico | Alta | Medio |
| 2.3 | Sem Soft Delete | Media | Medio |
| 2.4 | Risco de N+1 queries | Media | Medio |
| 3.1 | Sem logging estruturado | Media | Baixo |
| 3.3 | Baixa cobertura de testes | Media | Alto |
| 3.4 | Sem validacao de DTOs | Media | Medio |
| 4.3 | Sem health checks | Media | Baixo |
| 1.5 | Sem refresh token | Media | Alto |
| 3.2 | Sem audit trail | Baixa | Alto |
| 4.1 | Paginacao inconsistente | Baixa | Medio |
| 4.2 | Sem cache | Baixa | Medio |
| 5.1 | Sem OpenTelemetry | Baixa | Alto |
