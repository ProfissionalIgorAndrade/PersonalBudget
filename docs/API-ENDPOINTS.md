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
| `Bank` | `Itau`, `Nubank`, `Inter`, `Santander`, `Bradesco`, `Caixa` |
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
| `agency` | string |
| `accountNumber` | string |
| `initialBalance` | decimal |

**Resposta:** 201 com `data`: `{ "id": guid }`.

---

### `GET /api/accounts`

Lista contas do lar.

**Resposta `data`:** array de entidades `Account` (serializadas): `id`, `userId`, `householdId`, `bank`, `agency`, `number`, `balance`, `createdAt`, `isActive`, etc. (value objects podem aparecer aninhados conforme serialização).

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
| `agency` | string |
| `accountNumber` | string |

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

### `POST /api/credit-cards`

**Body:**

| Campo | Tipo |
|-------|------|
| `accountId` | guid (conta vinculada) |
| `name` | string |
| `limit` | decimal |
| `closingDay` | int |
| `dueDay` | int |

**Resposta:** 201 com `data`: `{ "id": guid }`.

---

### `GET /api/credit-cards`

**Resposta `data`:** lista de cartões (`CreditCard`).

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
| `periodStart` | datetime |
| `periodEnd` | datetime |
| `closingDate` | datetime |
| `dueDate` | datetime |
| `status` | string |
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
| `status` | string |
| `frequency` | string |
| `attributionProfileId` | guid |
| `correspondentDisplayName` | string |

**Com paginação:** `PaginatedStatementWithTransactionsResponse` — mesmos campos da fatura + `page`, `pageSize`, `totalCount`, `totalPages`.

---

### `POST /api/credit-cards/{creditCardId}/statements/{statementId}/close`

Marca fatura como fechada.

**Resposta `data`:** data/hora (ex.: `DateTime.Now` serializado).

---

### `POST /api/credit-cards/{creditCardId}/statements/{statementId}/pay`

Registra pagamento da fatura.

**Resposta `data`:** data/hora.

---

### `PUT /api/credit-cards/{creditCardId}`

**Body:**

| Campo | Tipo |
|-------|------|
| `name` | string |
| `limit` | decimal |
| `closingDay` | int |
| `dueDay` | int |

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
| `accountId` | guid \| null | Conta (método conta) |
| `categoryId` | guid \| null | |
| `creditCardId` | guid \| null | Cartão |
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
| `accountId` | guid |
| `accountName` | string |
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

## Checklist rápido para o front-end

1. Guardar JWT após login/signin; enviar `Authorization: Bearer …` em todas as rotas protegidas.
2. Enviar `X-Household-Id` quando o usuário trocar de lar (se a API for usada em modo multi-lar).
3. Tratar sempre o envelope `{ success, message, data }`.
4. Enums no JSON como **strings** (nomes dos enums acima).
5. Paginação: query `page` opcional; tamanho de página **15** para listagens paginadas de transações (lar e por conta) e fatura de cartão.
6. Datas em strings de criação de transação: aceitar **dd/MM/yyyy** ou **ISO** conforme mensagens de erro da API.

---

*Gerado a partir dos controllers em `PersonalBudget.Api` e DTOs da camada Application. Ajuste `API_BASE` e fluxo de household conforme ambiente.*
