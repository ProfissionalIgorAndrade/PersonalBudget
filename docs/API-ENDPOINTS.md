# PersonalBudget API — referência para o front-end

Base URL: `{API_BASE}` (ex.: `https://localhost:7xxx` ou URL de produção). Não há prefixo de versão no path.

---

## Convenções globais

### Autenticação

- Endpoints marcados como **autenticados** exigem header:
  - `Authorization: Bearer <JWT>`
- Obter o token em `POST /api/authentication/signin` ou `POST /api/authentication/login` (campo `data.token` no envelope, ver abaixo).

### Lar ativo (household)

- Na maioria dos endpoints autenticados, o backend resolve o **lar** pelo header opcional:
  - `X-Household-Id: <guid>`
- Se omitido, a aplicação usa a regra de negócio padrão (ex.: lar único do usuário ou perfil vinculado — conforme implementação do resolver).

### Formato JSON

- Request e response usam **camelCase** nos nomes de propriedades.
- **Enums** são serializados como **strings** com o nome do enum em C# (ex.: `"Income"`, `"Expense"`, `"Account"`, `"Pending"`). O `JsonStringEnumConverter` está habilitado.

### Envelope de resposta (`ApiResponse<T>`)

Todas as respostas de sucesso seguem este formato:

```json
{
  "success": true,
  "message": "string",
  "data": { }
}
```

Erros tratados pelo middleware costumam retornar:

```json
{
  "success": false,
  "message": "mensagem de erro",
  "data": null
}
```

- `success`: `boolean`
- `message`: texto (PT-BR em várias regras de negócio)
- `data`: payload tipado ou `null`

### Códigos HTTP

- **200** — sucesso (corpo com `success: true` em geral).
- **201** — recurso criado (`Location` pode apontar para ação relacionada).
- **400** — validação / regra de negócio (`DomainException`, `ArgumentException`, etc.).
- **401** — não autenticado (ex.: signin/login falhou).
- **403** — proibido (ex.: recurso não pertence ao usuário).
- **404** — não encontrado (alguns endpoints de fatura/cartão).
- **500** — erro interno.

---

## Enums (valores em string no JSON)

| Enum | Valores |
|------|--------|
| `Bank` | `Itau`, `Nubank`, `Inter`, `Santander`, `Bradesco`, `Caixa`, `BancoDoBrasil`, `Btg`, `C6Bank`, `Safra`, `Sicoob`, `Sicredi`, `Original`, `Pan`, `Neon`, `PicPay`, `MercadoPago`, `Banrisul`, `Next`, `Bmg`, `Xp`, `PagBank`, `Bv`, `Outro` |
| `CategoryType` | `Income`, `Expense` |
| `TransactionType` | `Income`, `Expense` |
| `TransactionFrequency` | `Variable`, `Fixed`, `Installments` |
| `PaymentMethod` | `Account`, `CreditCard`, `Cash`, `Transfer` |
| `TransactionStatus` | `Pending`, `Completed`, `Simulated`, `Cancelled` |

---

# Endpoints

## 1. Autenticação — `api/authentication`

Não requer `Authorization`.

### `POST /api/authentication/signin`

Cadastro + login.

**Body (JSON):**

| Campo | Tipo | Obrigatório |
|-------|------|-------------|
| `name` | string | sim |
| `email` | string | sim |
| `password` | string | sim |

**Resposta `data` (exemplo):** objeto com `userId` e `token` (JWT).

**Mensagem:** ex.: `"Usuário criado e autenticado."`

---

### `POST /api/authentication/login`

Login.

**Body:**

| Campo | Tipo |
|-------|------|
| `email` | string |
| `password` | string |

**Resposta `data`:** `LoginUserResponse` — propriedades `userId`, `token`.

---

## 2. Lares — `api/households`

Autenticado: `Authorization` + opcional `X-Household-Id: <guid>`.

### `GET /api/households`

Lista lares do usuário.

**Resposta `data`:** array de `HouseholdListItemDto`: `{ id: guid, name: string }`.

---

### `GET /api/households/{householdId}/profiles`

Perfis de correspondente (Igor, Família, etc.) para atribuição de lançamentos.

**Resposta `data`:** array de `HouseholdMemberProfileResponseDto`:

| Campo | Tipo |
|-------|------|
| `id` | guid |
| `displayName` | string |
| `kind` | string |
| `userId` | guid \| null |

---

### `POST /api/households/{householdId}/profiles`

Cria perfil de correspondente compartilhado (rótulo sem novo usuário).

**Body:**

| Campo | Tipo |
|-------|------|
| `displayName` | string |

**Resposta:** 201 com `data` = objeto criado (perfil) e mensagem de sucesso.

---

### `GET /api/households/{householdId}/summary/by-profile`

Resumo por correspondente no mês/ano.

**Query:**

| Parâmetro | Tipo |
|-----------|------|
| `month` | int |
| `year` | int |

**Resposta `data`:** array de `HouseholdProfileSummaryRow`:

| Campo | Tipo |
|-------|------|
| `profileId` | guid |
| `displayName` | string |
| `totalExpenses` | decimal |
| `totalIncome` | decimal |

---

### `POST /api/households/invites`

Cria convite para outro e-mail.

**Body:**

| Campo | Tipo |
|-------|------|
| `householdId` | guid |
| `inviteeEmail` | string |

**Resposta `data`:** `{ "token": "<string>" }` — enviar ao convidado para aceitar.

---

### `POST /api/households/invites/accept`

Aceita convite.

**Body:**

| Campo | Tipo |
|-------|------|
| `token` | string |

**Resposta `data`:** `null` com mensagem de sucesso.

---

## 3. Contas — `api/accounts`

Autenticado + `X-Household-Id` (opcional, conforme regra global).

### `POST /api/accounts`

**Body:**

| Campo | Tipo |
|-------|------|
| `bank` | `Bank` (string enum) |
| `memberId` | guid |
| `name` | string \| null (apelido opcional, máx. 80; vazio = sem apelido) |
| `initialBalance` | decimal |

**Resposta:** 201 com `data`: `{ "id": guid }`.

---

### `GET /api/accounts`

Lista contas do lar.

**Resposta `data`:** array de `AccountResponse`: `id`, `bank`, `balance`, `memberProfileId`, `memberName`, `displayName`, `isActive`, `createdAt`, `kind`, `parentAccountId`, `name` (apelido da conta corrente ou nome da caixinha), `savingsGoal`. Não há `agency` nem `accountNumber`.

`displayName`: caixinha = `name` ou "Caixinha"; conta corrente = (`name` ou banco) + ` - {membro}` quando há membro.

---

### `GET /api/accounts/{accountId}/transactions`

Transações da conta no mês/ano; **exclui** lançamentos de `PaymentMethod.CreditCard` (regra de negócio).

**Query:**

| Parâmetro | Tipo | Obrigatório |
|-----------|------|-------------|
| `month` | int | sim |
| `year` | int | sim |
| `page` | int | não — se omitido, retorna lista completa; se informado, paginação |

- `page` ≥ 1 quando usado.

**Resposta sem `page`:** `data` = array de `GetAllTransactionByUserResponse` (ver secção Transações).

**Com `page`:** `data` = `PaginatedTransactionsResult`:

| Campo | Tipo |
|-------|------|
| `items` | array de transações |
| `page` | int |
| `pageSize` | int (fixo 15 no serviço) |
| `totalCount` | int |
| `totalPages` | int (calculado) |

---

### `PUT /api/accounts/{accountId}`

**Body:**

| Campo | Tipo |
|-------|------|
| `bank` | `Bank` |
| `memberId` | guid \| null |
| `name` | string \| null (apelido; vazio remove na conta corrente) |

**Resposta `data`:** `null`.

---

### `DELETE /api/accounts/{accountId}`

**Resposta `data`:** `null`.

---

## 4. Categorias — `api/categories`

### `POST /api/categories`

**Body:**

| Campo | Tipo |
|-------|------|
| `name` | string |
| `type` | `CategoryType` |

**Resposta:** 201 com `data`: `{ "id": guid }`.

---

### `GET /api/categories`

**Resposta `data`:** lista de categorias do lar (entidade `Category`).

---

### `PUT /api/categories/{id}`

**Body:** o DTO inclui `categoryId`, `name`, `type` no backend; o **id da URL** é o que vale para o comando de atualização. Enviar pelo menos `name` e `type` alinhados ao contrato.

| Campo | Tipo |
|-------|------|
| `categoryId` | guid (pode existir no tipo; preferir consistência com `{id}` da URL) |
| `name` | string |
| `type` | `CategoryType` |

---

### `DELETE /api/categories/{id}`

**Resposta `data`:** `null`.

---

## 5. Cartões de crédito — `api/credit-cards`

O cartão não tem conta de débito nem dia de fechamento. A fatura é identificada por (cartão, mês, ano), onde mês/ano são os do **vencimento**, e existe uma única por combinação. Compras de cartão não pertencem a nenhuma conta: não alteram o saldo e vêm com `accountId` nulo.

### `POST /api/credit-cards`

**Body:**

| Campo | Tipo |
|-------|------|
| `name` | string |
| `limit` | decimal (> 0) |
| `dueDay` | int (1 a 31) |
| `color` | string \| null |
| `memberId` | guid \| null — perfil de membro dono do cartão |

**Resposta:** 201 com `data`: `{ "id": guid }`.

---

### `GET /api/credit-cards`

**Resposta `data`:** lista de cartões ativos do lar (`CreditCard`): `id`, `userId`, `householdId`, `memberId`, `name`, `limit`, `dueDay`, `isActive`, `color`.

---

### `GET /api/credit-cards/{creditCardId}/statement`

Fatura do cartão com lançamentos para o mês/ano.

**Query:**

| Parâmetro | Tipo |
|-----------|------|
| `month` | int |
| `year` | int |
| `page` | int opcional — sem `page`, lista completa; com `page`, paginação (15 itens por página) |

- **404** se cartão inexistente ou fatura inexistente para o período — `success: false` no padrão da API.

**Resposta `data` (sem paginação):** `StatementWithTransactionsResponse`:

| Campo | Tipo |
|-------|------|
| `statementId` | guid |
| `creditCardId` | guid |
| `creditCardName` | string |
| `limit` | decimal |
| `dueDate` | datetime — calculado: `dueDay` do cartão no mês da fatura, limitado ao último dia do mês |
| `totalAmount` | decimal |
| `transactions` | array de `StatementTransactionItemDto` |

`StatementTransactionItemDto`:

| Campo | Tipo |
|-------|------|
| `id` | guid |
| `date` | datetime |
| `dueDate` | datetime \| null |
| `description` | string |
| `amount` | decimal |
| `categoryId` | guid \| null |
| `categoryName` | string \| null |
| `transactionType` | string |
| `frequency` | string |
| `attributionProfileId` | guid |
| `correspondentDisplayName` | string |
| `observations` | string \| null |
| `reviewed` | bool — lançamento marcado como revisado |

**Com paginação:** `PaginatedStatementWithTransactionsResponse` — mesmos campos da fatura + `page`, `pageSize`, `totalCount`, `totalPages`.

---

### `PATCH /api/credit-cards/{creditCardId}/statement/{statementId}/reviewed`

Marca ou desmarca como revisados **todos** os lançamentos da fatura.

**Body:**

| Campo | Tipo |
|-------|------|
| `reviewed` | bool |

- **400** se o cartão não pertencer ao lar ativo ou a fatura não pertencer ao cartão.

**Resposta `data`:** `null`.

---

### `PUT /api/credit-cards/{creditCardId}`

**Body:**

| Campo | Tipo |
|-------|------|
| `name` | string |
| `limit` | decimal |
| `dueDay` | int (1 a 31) |
| `color` | string \| null |
| `memberId` | guid \| null — null mantém o atual |

---

### `DELETE /api/credit-cards/{creditCardId}`

Exclui cartão.

---

## 6. Transações — `api/transactions`

### `POST /api/transactions`

Cria transação (conta, cartão, transferência, parcelas, recorrência conforme combinação de campos).

**Body (principais campos):**

| Campo | Tipo | Notas |
|-------|------|--------|
| `accountId` | guid \| null | Conta. Obrigatória exceto em compra de cartão, que não tem conta |
| `categoryId` | guid \| null | |
| `creditCardId` | guid \| null | Cartão |
| `statementMonth` / `statementYear` | int \| null | Obrigatórios em compra de cartão: mês/ano (do vencimento) da fatura; a fatura é criada se não existir |
| `fromAccountId` / `toAccountId` | guid \| null | Transferência |
| `type` | `TransactionType` | |
| `frequency` | `TransactionFrequency` | Parcelas exigem `Installments` + `installmentCount` > 1 e cartão |
| `paymentMethod` | `PaymentMethod` | |
| `amount` | decimal | |
| `date` | string | dd/MM/yyyy ou ISO |
| `description` | string | |
| `autoComplete` | bool | Conta: concluir e aplicar saldo quando `true` |
| `installmentCount` | int \| null | |
| `totalAmount` | decimal \| null | Parcelado: total da compra |
| `title` | string \| null | Título/descrição exibida em parcelas |
| `expirationDate` | string \| null | Recorrência fixa |
| `dueDate` | string \| null | |
| `dueDay` | int \| null | Recorrência |
| `repeatCount` | int \| null | Quantidade de meses (recorrente) |
| `attributionProfileId` | guid \| null | Correspondente; default = perfil do usuário no lar |
| `status` | `TransactionStatus` \| null | Opcional: status inicial; se omitido, mantém regras atuais (`autoComplete`, etc.) |

**Resposta:** 201 com `data`: `{ "transactionId": guid }` (transferência pode retornar id de vínculo conforme estratégia).

---

### `GET /api/transactions`

Todas as transações do lar (lista).

**Resposta `data`:** array de `GetAllTransactionByUserResponse`:

| Campo | Tipo |
|-------|------|
| `id` | guid |
| `accountId` | guid \| null — nulo em compra de cartão |
| `categoryId` | guid \| null |
| `categoryName` | string \| null |
| `categoryType` | string \| null |
| `creditCardId` | guid \| null |
| `creditCardName` | string \| null |
| `transferId` | guid \| null |
| `type` | string |
| `status` | string |
| `paymentMethod` | string |
| `frequency` | string |
| `expirationDate` | datetime \| null |
| `dueDate` | datetime \| null |
| `amount` | decimal |
| `date` | datetime |
| `description` | string |
| `attributionProfileId` | guid |
| `correspondentDisplayName` | string |
| `recurrenceId` | guid \| null |
| `statementMonth` | int \| null |
| `statementYear` | int \| null |
| `observations` | string \| null |
| `reviewed` | bool — lançamento marcado como revisado |

---

### `GET /api/transactions/id/{transactionId}`

Detalhe de uma transação (entidade `Transaction` serializada).

---

### `GET /api/transactions/month/{month}/year/{year}`

Transações do lar no mês/ano.

**Query:**

| Parâmetro | Tipo |
|-----------|------|
| `page` | int opcional — sem `page`, lista completa; com `page`, paginação (15 por página) |

**Resposta:** lista ou `PaginatedTransactionsResult` (igual contas).

---

### `PATCH /api/transactions/{transactionId}`

Atualização parcial. **Não aplicável** a: transações **concluídas**, **cartão de crédito**, **transferência**.

**Body — todos opcionais; `null` = não alterar; strings vazias em datas podem limpar:**

| Campo | Tipo |
|-------|------|
| `amount` | decimal \| null |
| `date` | string \| null |
| `description` | string \| null |
| `categoryId` | guid \| null |
| `dueDate` | string \| null |
| `expirationDate` | string \| null |
| `attributionProfileId` | guid \| null |

---

### `PATCH /api/transactions/{transactionId}/reviewed`

Marca ou desmarca um lançamento como revisado. Não passa pelas regras de edição, então vale para qualquer lançamento.

**Body:**

| Campo | Tipo |
|-------|------|
| `reviewed` | bool |

- **400** se a transação não existir ou pertencer a outro lar.

**Resposta `data`:** `null`.

---

### `DELETE /api/transactions/{transactionId}`

Exclui uma transação (não exclui **Completed** — entra em `skipped`).

**Resposta `data`:**

| Campo | Tipo |
|-------|------|
| `deletedCount` | int |
| `skippedCount` | int |
| `skippedIds` | array de guid |

---

### `PATCH /api/transactions/{transactionId}/status`

Atualiza apenas o status.

**Body:**

| Campo | Tipo |
|-------|------|
| `status` | `TransactionStatus` |

---

### `DELETE /api/transactions/batch`

**Body:**

| Campo | Tipo |
|-------|------|
| `transactionIds` | array de guid |

**Resposta `data`:** mesmo formato que delete único (`deletedCount`, `skippedCount`, `skippedIds`).

---

## 7. Simulador — `api/simulator`

Todos os endpoints são autenticados e usam o lar ativo (`X-Household-Id`). Nenhum persiste dados: o servidor não guarda cenários.

> As simulações salvas ficam em `api/simulations` (seção 8). `POST /api/simulator/calculate` e `POST /api/simulator/projection` continuam sem uso desse armazenamento: recebem os impactos no corpo e não leem nada salvo.

### `POST /api/simulator/projection`

Projeta saldo e fluxo mês a mês a partir do saldo real das contas correntes, somando ao baseline os impactos enviados. Tudo é calculado em SQL agregado (nunca carrega o mês inteiro em memória).

**Body:**

| Campo | Tipo | Notas |
|-------|------|--------|
| `today` | string \| null | `yyyy-MM-dd`. Omitido = data do servidor em UTC-3. Define o mês de referência e o corte do saldo de partida |
| `months` | int | Horizonte, **1 a 24** (inclui o mês de referência) |
| `impacts` | array \| null | **Máx. 50** |

Cada item de `impacts`:

| Campo | Tipo | Notas |
|-------|------|--------|
| `id` | string \| null | Devolvido como veio; omitido = `impact-{posição}` |
| `description` | string \| null | Até 120 caracteres |
| `type` | `Income` \| `Expense` | |
| `mode` | `Single` \| `Installment` \| `Monthly` | |
| `startMonth` | string | `yyyy-MM`, ano entre 2000 e 2100. Precisão de mês, não de dia |
| `amount` | decimal | Maior que 0 |
| `amountKind` | `PerInstallment` \| `Total` \| null | Só vale em `Installment`; omitido = `PerInstallment` |
| `installments` | int \| null | `Installment`: **1 a 120** (obrigatório) |
| `months` | int \| null | `Monthly`: duração (1 a 120). `null` ou `0` = até o fim do horizonte |

Regra de parcela (`Installment`): `PerInstallment` usa `amount` em cada parcela; `Total` usa `Round(amount / installments, 2)` e o **resto vai na última parcela** (igual à criação real de parcelas de cartão).

**Resposta `data`:**

| Campo | Notas |
|-------|--------|
| `referenceMonth` | `yyyy-MM` do mês de `today` |
| `openingBalance` | `{ amount, asOf, accounts: [{ id, name, balance }], excludesSavings: true }`. Soma do saldo **de hoje** (lançamentos com data até `today`) das contas correntes **ativas**; caixinhas ficam de fora |
| `assumptions` | `{ lookbackMonths (3), monthsWithData, averageIncome \| null, averageVariableExpense \| null, notes: string[] }`. `notes` são textos em pt-BR explicando cada regra |
| `baseline[]` | Um item por mês: `year, month, label ("out/26"), income, committed, variable, result, balance` |
| `impacts[]` | `id, description, type, mode, monthly[], totalInHorizon, totalFull, installmentAmount, lastInstallmentAmount, installmentsInHorizon, installmentsTotal`. `monthly` é assinado (receita positiva, despesa negativa) e alinhado ao horizonte. `installmentAmount` e `lastInstallmentAmount` só vêm em `Installment`. `installmentsTotal` é `null` em `Monthly` sem duração |
| `scenario[]` | Um item por mês: `year, month, label, simulatedIncome, simulatedExpense (positivo), result, balance, delta (saldo do cenário menos saldo do baseline)` |
| `summary` | `baselineMinBalance { amount, monthIndex }`, `scenarioMinBalance { amount, monthIndex }`, `firstNegativeMonthIndexBaseline`, `firstNegativeMonthIndexScenario` (`null` se nunca fica negativo), `endBalanceBaseline`, `endBalanceScenario`, `totalImpactInHorizon`, `totalImpactFull` (assinados) |
| `warnings[]` | `{ impactId, code, message }` com `code` = `BeforeWindow` (termina antes do mês de referência), `Truncated` (começa antes: só os meses da janela entram) ou `AfterWindow` (continua depois do horizonte ou começa depois dele: `totalFull` inclui o que o horizonte não mostra) |

**Como o baseline é calculado** (por mês M, sempre sem `Transfer` e `Savings`; compras de cartão pelo mês/ano da fatura, o resto pela data):

- `committed` = despesas `Fixed` + `Installments` já lançadas em M (inclui a fatura do cartão).
- `variable` = despesas `Variable` já lançadas em M + o que faltar para chegar à média (`max(0, averageVariableExpense - lançado)`), ou seja, `max(lançado, média)`.
- `income` = receitas já lançadas em M + `max(0, averageIncome - lançado)`.
- Médias: média dos **3 meses completos anteriores** ao mês de referência; meses sem nenhum lançamento são ignorados; sem dados a média é `null` e a estimativa é 0.
- `result` = `income - committed - variable`; `balance` = saldo do mês anterior + `result`, partindo do `openingBalance`.
- **Mês de referência**: lançamentos de conta com data até `today` já estão no saldo de partida, então não entram de novo no fluxo (mas contam como "já lançado" para reduzir as estimativas). Entram: a fatura inteira do cartão do mês, lançamentos de conta com data depois de `today` e o restante estimado.
- Cenário = baseline + todos os impactos enviados.

**`fullMonth`** (`{ income, expense, result }`, aditivo): o mês inteiro, e não o fluxo restante. Sempre sem `Transfer` e `Savings`.

- **Meses depois do de referência**: igual aos campos acima: `income = income`, `expense = committed + variable`, `result = income - expense`.
- **Mês de referência**: inclui também o que já foi lançado até `today` (lançamentos de conta com data até hoje e a fatura do cartão), que `income`, `committed` e `variable` deixam de fora por já estarem no saldo de partida. `income = lançado + max(0, averageIncome - lançado)` (ou seja, `max(lançado, média)`); `expense = comprometido lançado + variável lançado + max(0, averageVariableExpense - variável lançado)`; `result = income - expense`. Aqui "lançado" soma todas as linhas do mês, inclusive as de data até `today`.
- Sem histórico, a média é `null` e a estimativa é 0.
- `balance` e o restante dos campos continuam sendo o fluxo restante a partir do saldo de partida; `fullMonth` não entra no saldo corrido.

**Limites e premissas:** despesas fixas futuras só existem até onde foram lançadas; a estimativa variável usa a média de 3 meses e inclui compras grandes pontuais; compra de cartão sai no mês da fatura; o mês atual é parcial; receita de cartão (estorno) conta como receita do mês da fatura.

**Erros:** `400` com envelope `{ success: false, message }` quando: horizonte fora de 1..24, mais de 50 impactos, `today` ou `startMonth` inválidos, `amount` ≤ 0, `installments` fora de 1..120, `months` do impacto fora de 0..120, descrição acima de 120 caracteres. A mensagem diz qual impacto falhou, ex.: `Impacto #2 ("Viagem"): o valor deve ser maior que zero.` Enum desconhecido (`type`, `mode`, `amountKind`) ou corpo ausente também retornam `400` (validação padrão do ASP.NET, corpo no formato `ProblemDetails`).

**Exemplo:**

```json
POST /api/simulator/projection
{
  "today": "2026-10-15",
  "months": 6,
  "impacts": [
    { "id": "a", "description": "Notebook", "type": "Expense", "mode": "Installment",
      "startMonth": "2026-11", "amount": 3600, "amountKind": "Total", "installments": 12 }
  ]
}
```

---

### `POST /api/simulator/calculate` (legado)

> **Legado — será removido** quando o frontend migrar para `POST /api/simulator/projection`. Não use em telas novas: parte de saldo zero, repete o mês atual igual por N meses e usa agregados que ainda incluem transferências e movimentos de caixinha.

Query `months` (1 a 24, padrão 6). Body: `{ scenarioName, impacts: [{ description, amount, type, mode, startDate, installmentCount }] }`.

---

## 8. Simulações salvas — `api/simulations`

Simulações "E se...?" guardadas no servidor, **por lar**. Todo membro do lar ativo (`X-Household-Id`) vê todas as simulações do lar; quem cria é o **dono**, e **só o dono edita ou apaga**. O liga/desliga de cada simulação é pessoal e fica só no cliente (nunca vai para o servidor). Todos os endpoints são autenticados.

**Item** (`data` de `GET`): `id`, `description`, `type`, `mode`, `startMonth`, `amount`, `amountKind`, `installments`, `months`, `ownerUserId`, `ownerName`, `isOwner`, `createdAt`, `updatedAt`.

- `type`: `Income` \| `Expense`. `mode`: `Single` \| `Installment` \| `Monthly`. `amountKind`: `PerInstallment` \| `Total` (strings).
- `ownerName`: nome do perfil vinculado do dono no lar; se não houver, o nome do usuário; senão `"Membro"`. Nomes podem repetir: use `ownerUserId` para identificar o dono. `isOwner` é `true` quando o dono é o usuário autenticado.
- Ordem: `createdAt` crescente e, em empate, `id`. Estável entre aparelhos.
- `installments` só vem preenchido em `Installment`; `months` só em `Monthly` (`null` = até o fim do horizonte).

**Body** de `POST` e `PUT /{id}` (e de cada item de `import`):

| Campo | Tipo | Notas |
|-------|------|--------|
| `description` | string \| null | Até **120** caracteres (após trim); vazio é aceito |
| `type` | string | `Income` ou `Expense` |
| `mode` | string | `Single`, `Installment` ou `Monthly` |
| `startMonth` | string | `yyyy-MM`, ano de **2000 a 2100** |
| `amount` | decimal | **> 0** |
| `amountKind` | string \| null | Omitido = `PerInstallment` |
| `installments` | int \| null | **Obrigatório, 1 a 120**, em `Installment`; ignorado nos demais modos |
| `months` | int \| null | Só em `Monthly`: **0 a 120**; `null`/`0` = até o fim do horizonte; ignorado nos demais modos |

### `GET /api/simulations`

Lista as simulações do lar ativo. **Resposta `data`:** array de itens (acima).

### `POST /api/simulations`

Cria uma simulação para o usuário autenticado. **201** com `data: { id }`. O id é gerado pelo servidor.

### `PUT /api/simulations/{id}`

Substitui os campos editáveis (mesmo body do `POST`). **200**, `data: null`. Só o dono.

### `DELETE /api/simulations/{id}`

Apaga uma simulação. **200**, `data: null`. Só o dono.

### `DELETE /api/simulations/mine`

Apaga **só as simulações do usuário autenticado** no lar ativo; nunca as dos outros membros. **200**, `data: { removed }`.

### `POST /api/simulations/import`

Cria várias simulações de uma vez para o usuário autenticado (ex.: as que estavam no `localStorage`). **Tudo ou nada**: se um item for inválido ou estourar o limite, nada é criado. **Body:** `{ "simulations": [ ...mesmo formato do POST... ] }` (1 a 50 itens). Os itens mantêm a ordem recebida. **200**, `data: { imported, ids }`.

**Limite:** **50 simulações por dono** em cada lar (as existentes contam na importação).

**Erros:**

- `400` com `{ success: false, message }`: validação (descrição > 120, `startMonth` inválido ou fora de 2000..2100, `amount` ≤ 0, `installments` fora de 1..120 em `Installment`, `months` fora de 0..120 em `Monthly`, `type`/`mode`/`amountKind` ausente ou inválido), limite de 50 por dono, importação vazia. A mensagem diz qual simulação falhou, ex.: `Simulação #2 ("Viagem"): o valor deve ser maior que zero.` (o `#N` só aparece na importação). Nome de enum desconhecido ou corpo ausente também retornam `400`/`415` pela validação padrão do ASP.NET (`ProblemDetails`).
- `400` `Simulação não encontrada.`: id inexistente **ou de outro lar**.
- `403` `Simulação não pertence ao usuário.`: `PUT`/`DELETE` por quem não é o dono.

**Aceitar convite:** ao aceitar um convite (`POST /api/households/invites/accept`), as simulações do lar de origem de quem aceita vão para o lar de destino e continuam sendo dele (`ownerUserId` não muda). Ao mesclar dois perfis vinculados (`DeleteProfileAndMergeAsync`), as simulações do perfil removido passam para o dono de destino.

**Exemplo:**

```json
POST /api/simulations
{ "description": "Notebook", "type": "Expense", "mode": "Installment",
  "startMonth": "2026-11", "amount": 3600, "amountKind": "Total", "installments": 12 }

201 { "success": true, "message": "Simulação criada.", "data": { "id": "..." } }
```

---

## Checklist rápido para o front-end

1. Guardar JWT após login/signin; enviar `Authorization: Bearer …` em todas as rotas protegidas.
2. Enviar `X-Household-Id` quando o usuário trocar de lar (se a API for usada em modo multi-lar).
3. Tratar sempre o envelope `{ success, message, data }`.
4. Enums no JSON como **strings** (nomes dos enums acima).
5. Paginação: query `page` opcional; tamanho de página **15** para listagens paginadas de transações (lar e por conta) e fatura de cartão.
6. Datas em strings de criação de transação: aceitar **dd/MM/yyyy** ou **ISO** conforme mensagens de erro da API.

---

*Gerado a partir dos controllers em `PersonalBudget.Api` e DTOs da camada Application. Ajuste `API_BASE` e fluxo de household conforme ambiente.*
