using PersonalBudget.Application.DTOs.CreditCard;
using PersonalBudget.Application.Interfaces;

public class CreditCardStatementService : ICreditCardStatementService
{
    public const int StatementTransactionsPageSize = 15;

    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICreditCardStatementRepository _statementRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionQueryRepository _transactionQueryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IHouseholdMemberProfileRepository _profileRepository;

    public CreditCardStatementService(
        ICreditCardRepository creditCardRepository,
        ICreditCardStatementRepository statementRepository,
        IAccountRepository accountRepository,
        ITransactionQueryRepository transactionQueryRepository,
        ITransactionRepository transactionRepository,
        IHouseholdMemberProfileRepository profileRepository)
    {
        _creditCardRepository = creditCardRepository;
        _statementRepository = statementRepository;
        _accountRepository = accountRepository;
        _transactionQueryRepository = transactionQueryRepository;
        _transactionRepository = transactionRepository;
        _profileRepository = profileRepository;
    }

    public async Task<List<CreditCardStatementDto>> GetByCreditCardAsync(Guid creditCardId)
    {
        var statements = await _statementRepository.GetByCreditCardAsync(creditCardId);

        return statements.Select(x => new CreditCardStatementDto(
            x.Id,
            x.CreditCardId,
            x.PeriodStart,
            x.PeriodEnd,
            x.ClosingDate,
            x.DueDate,
            x.TotalAmount.Amount,
            x.Status.ToString()
        )).ToList();
    }

    public async Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsAsync(Guid householdId, Guid creditCardId, int month, int year)
    {
        var card = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (card is null || card.HouseholdId != householdId)
            return null;

        var statement = await _statementRepository.GetByCreditCardAndClosingMonthYearAsync(creditCardId, month, year);
        if (statement is null)
            return null;

        var transactions = await _transactionQueryRepository.GetTransactionDetailsByStatementAsync(creditCardId, statement.PeriodEnd);
        var netTotal = await _transactionQueryRepository.GetStatementNetTotalAsync(creditCardId, statement.PeriodEnd);
        var dueDate = ComputeDueDate(statement.ClosingDate, card.ClosingDay, card.DueDay);

        return new StatementWithTransactionsResponse(
            statement.Id,
            card.Id,
            card.Name,
            card.Limit,
            statement.PeriodStart,
            statement.PeriodEnd,
            statement.ClosingDate,
            dueDate,
            statement.Status.ToString(),
            netTotal,
            transactions
        );
    }

    public async Task<PaginatedStatementWithTransactionsResponse?> GetStatementWithTransactionsPagedAsync(
        Guid householdId, Guid creditCardId, int month, int year, int page, int pageSize)
    {
        if (page < 1)
            throw new DomainException("Page must be at least 1.");

        var card = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (card is null || card.HouseholdId != householdId)
            return null;

        var statement = await _statementRepository.GetByCreditCardAndClosingMonthYearAsync(creditCardId, month, year);
        if (statement is null)
            return null;

        var (transactions, totalCount) = await _transactionQueryRepository.GetTransactionDetailsByStatementPagedAsync(
            creditCardId, statement.PeriodEnd, page, pageSize);
        var netTotal = await _transactionQueryRepository.GetStatementNetTotalAsync(creditCardId, statement.PeriodEnd);
        var dueDate = ComputeDueDate(statement.ClosingDate, card.ClosingDay, card.DueDay);

        return new PaginatedStatementWithTransactionsResponse(
            statement.Id,
            card.Id,
            card.Name,
            card.Limit,
            statement.PeriodStart,
            statement.PeriodEnd,
            statement.ClosingDate,
            dueDate,
            statement.Status.ToString(),
            netTotal,
            transactions,
            page,
            pageSize,
            totalCount
        );
    }

    /// <summary>
    /// Estorna o pagamento de uma fatura paga: credita o valor na conta de origem,
    /// reverte as transações para pendente, cria um lançamento de estorno visível
    /// e muda o status para Fechada (Paid → Closed).
    /// O AddAsync salva tudo em uma única operação ao final.
    /// </summary>
    private async Task ApplyPaymentReversalAsync(
        CreditCard card,
        CreditCardStatement statement,
        UpdateStatementStatusCommand command)
    {
        if (statement.PaidFromAccountId is null)
            throw new DomainException("Não é possível estornar: conta de pagamento não registrada na fatura.");

        var refundAccount = await _accountRepository.GetByIdAsync(statement.PaidFromAccountId.Value);
        if (refundAccount is null)
            throw new DomainException("Conta de pagamento não encontrada.");

        if (refundAccount.HouseholdId != command.HouseholdId)
            throw new DomainException("Conta não pertence ao lar.");

        var profile = await _profileRepository.GetLinkedProfileForUserAsync(command.HouseholdId, command.UserId);
        if (profile is null)
            throw new DomainException("Perfil do usuário não encontrado no lar.");

        var refundAmount = new Money(statement.TotalAmount.Amount);
        var description = $"Estorno de pagamento de fatura - {card.Name} ({statement.ClosingMonth:D2}/{statement.ClosingYear})";

        var today = DateTime.UtcNow;
        var maxDay = DateTime.DaysInMonth(statement.ClosingYear, statement.ClosingMonth);
        var refundDate = new DateTime(
            statement.ClosingYear,
            statement.ClosingMonth,
            Math.Min(today.Day, maxDay),
            0, 0, 0, DateTimeKind.Utc);

        var refundTransaction = Transaction.Create(
            userId: command.UserId,
            householdId: command.HouseholdId,
            attributionProfileId: profile.Id,
            accountId: refundAccount.Id,
            amount: refundAmount,
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            date: refundDate,
            description: description,
            initialStatus: TransactionStatus.Completed);

        // ── 1. Todas as mutações de domínio ──────────────────────────────────────
        refundAccount.Credit(refundAmount);

        var statementTransactions = await _transactionRepository.GetByStatementIdAsync(statement.Id);
        foreach (var t in statementTransactions)
        {
            if (t.Status == TransactionStatus.Completed)
                t.RevertToPending();
        }

        card.ReversePaymentStatement(statement.Id);          // Paid → Closed
        statement.SetRefundTransactionId(refundTransaction.Id);

        // ── 2. Persiste APENAS as mudanças de domínio (conta, fatura, transações revertidas) ──
        // refundTransaction ainda NÃO está no contexto, portanto não será inserido aqui.
        await _accountRepository.SaveChangesAsync();

        // ── 3. Insere o lançamento de estorno isoladamente ───────────────────────
        // AddAsync adiciona ao contexto e chama SaveChangesAsync.
        // Neste ponto o contexto só tem o refundTransaction para inserir.
        await _transactionRepository.AddAsync(refundTransaction);
    }

    private static DateTime ComputeDueDate(DateTime closingDate, int closingDay, int dueDay)
    {
        var year = closingDate.Year;
        var month = closingDate.Month;
        int dueYear, dueMonth;
        if (dueDay >= closingDay)
        {
            dueYear = year;
            dueMonth = month;
        }
        else
        {
            dueMonth = month + 1;
            dueYear = year;
            if (dueMonth > 12) { dueMonth = 1; dueYear++; }
        }
        var maxDay = DateTime.DaysInMonth(dueYear, dueMonth);
        var day = Math.Min(dueDay, maxDay);
        return DateTime.SpecifyKind(new DateTime(dueYear, dueMonth, day), DateTimeKind.Utc);
    }

    public async Task UpdateStatusAsync(UpdateStatementStatusCommand command)
    {
        var card = await _creditCardRepository.GetByIdWithStatementsAsync(command.CreditCardId);

        if (card is null)
            throw new ApplicationException("Cartão não encontrado.");

        if (card.HouseholdId != command.HouseholdId)
            throw new ApplicationException("Cartão não pertence ao lar.");

        switch (command.Status)
        {
            case BillStatus.Open:
                {
                    var statement = await _statementRepository.GetByIdAsync(command.StatementId);
                    if (statement is null)
                        throw new ApplicationException("Fatura não encontrada.");

                    // Paid → Closed (com estorno) → Open
                    if (statement.Status == BillStatus.Paid)
                        await ApplyPaymentReversalAsync(card, statement, command);
                    // AddAsync dentro de ApplyPaymentReversalAsync já salvou Paid → Closed

                    // Closed → Open (sem movimentação financeira)
                    card.ReopenStatement(command.StatementId);
                    await _creditCardRepository.SaveChangesAsync();
                    break;
                }

            case BillStatus.Closed:
                {
                    var statement = await _statementRepository.GetByIdAsync(command.StatementId);
                    if (statement is null)
                        throw new ApplicationException("Fatura não encontrada.");

                    if (statement.Status == BillStatus.Paid)
                        // AddAsync dentro de ApplyPaymentReversalAsync salva tudo (Paid → Closed)
                        await ApplyPaymentReversalAsync(card, statement, command);
                    else
                    {
                        card.CloseStatement(command.StatementId);
                        await _creditCardRepository.SaveChangesAsync();
                    }
                    break;
                }

            case BillStatus.Paid:
                {
                    var statement = await _statementRepository.GetByIdAsync(command.StatementId);
                    if (statement is null)
                        throw new ApplicationException("Fatura não encontrada.");

                    var accountId = command.AccountId ?? card.AccountId;
                    var account = await _accountRepository.GetByIdAsync(accountId);
                    if (account is null)
                        throw new DomainException("Conta não encontrada.");

                    if (account.HouseholdId != command.HouseholdId)
                        throw new DomainException("Conta não pertence ao lar.");

                    var profile = await _profileRepository.GetLinkedProfileForUserAsync(command.HouseholdId, command.UserId);
                    if (profile is null)
                        throw new DomainException("Perfil do usuário não encontrado no lar.");

                    // ── 1. Remove estorno anterior (DeleteManyAsync salva imediatamente) ──
                    if (statement.RefundTransactionId.HasValue)
                    {
                        var refundTx = await _transactionRepository.GetByIdAsync(statement.RefundTransactionId.Value);
                        if (refundTx is not null)
                            await _transactionRepository.DeleteManyAsync([refundTx]);
                    }

                    // ── 2. Todas as mutações de domínio ──────────────────────────────────
                    statement.ClearRefundTransactionId();

                    var today = DateTime.UtcNow;
                    var maxDay = DateTime.DaysInMonth(statement.ClosingYear, statement.ClosingMonth);
                    var paymentDate = new DateTime(
                        statement.ClosingYear,
                        statement.ClosingMonth,
                        Math.Min(today.Day, maxDay),
                        0, 0, 0, DateTimeKind.Utc);

                    var paymentAmount = new Money(card.PayStatement(statement.Id, accountId));
                    var paymentDescription = $"Pagamento de fatura - {card.Name} ({statement.ClosingMonth:D2}/{statement.ClosingYear})";

                    var paymentTransaction = Transaction.Create(
                        userId: command.UserId,
                        householdId: command.HouseholdId,
                        attributionProfileId: profile.Id,
                        accountId: account.Id,
                        amount: paymentAmount,
                        type: TransactionType.Expense,
                        paymentMethod: PaymentMethod.Account,
                        date: paymentDate,
                        description: paymentDescription,
                        initialStatus: TransactionStatus.Completed);

                    account.Debit(paymentAmount);

                    var statementTransactions = await _transactionRepository.GetByStatementIdAsync(statement.Id);
                    foreach (var t in statementTransactions)
                    {
                        if (t.Status == TransactionStatus.Pending)
                            t.Complete();
                    }

                    // ── 3. Persiste APENAS as mudanças de domínio (fatura, conta, transações) ──
                    // paymentTransaction ainda NÃO está no contexto, portanto não será inserido aqui.
                    await _accountRepository.SaveChangesAsync();

                    // ── 4. Insere o lançamento de pagamento isoladamente ──────────────────
                    await _transactionRepository.AddAsync(paymentTransaction);
                    break;
                }

            default:
                throw new DomainException($"Status inválido: {command.Status}.");
        }
    }
}