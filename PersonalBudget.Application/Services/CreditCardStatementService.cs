using PersonalBudget.Application.DTOs.CreditCard;
using PersonalBudget.Application.Interfaces;

public class CreditCardStatementService : ICreditCardStatementService
{
    public const int StatementTransactionsPageSize = 15;

    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICreditCardStatementRepository _statementRepository;
    private readonly ITransactionQueryRepository _transactionQueryRepository;
    private readonly ITransactionRepository _transactionRepository;

    public CreditCardStatementService(
        ICreditCardRepository creditCardRepository,
        ICreditCardStatementRepository statementRepository,
        ITransactionQueryRepository transactionQueryRepository,
        ITransactionRepository transactionRepository)
    {
        _creditCardRepository = creditCardRepository;
        _statementRepository = statementRepository;
        _transactionQueryRepository = transactionQueryRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task SetReviewedAsync(Guid householdId, Guid creditCardId, Guid statementId, bool reviewed)
    {
        var card = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (card is null || card.HouseholdId != householdId)
            throw new DomainException("Cartão não encontrado.");

        var statement = await _statementRepository.GetByIdAsync(statementId);
        if (statement is null || statement.CreditCardId != creditCardId)
            throw new DomainException("Fatura não encontrada.");

        var transactions = (await _transactionRepository.GetByStatementIdAsync(statement.Id)).ToList();
        foreach (var transaction in transactions)
            transaction.SetReviewed(reviewed);

        await _transactionRepository.BulkUpdateAsync(transactions);
    }

    public Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsAsync(
        Guid householdId, Guid creditCardId, int month, int year)
        => GetStatementAsync(
            householdId, creditCardId,
            () => _statementRepository.GetByCreditCardAndMonthYearAsync(creditCardId, month, year));

    public Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsByIdAsync(
        Guid householdId, Guid creditCardId, Guid statementId)
        => GetStatementAsync(
            householdId, creditCardId,
            () => _statementRepository.GetByIdAsync(statementId));

    public Task<PaginatedStatementWithTransactionsResponse?> GetStatementWithTransactionsPagedAsync(
        Guid householdId, Guid creditCardId, int month, int year, int page, int pageSize)
        => GetStatementPagedAsync(
            householdId, creditCardId,
            () => _statementRepository.GetByCreditCardAndMonthYearAsync(creditCardId, month, year),
            page, pageSize);

    public Task<PaginatedStatementWithTransactionsResponse?> GetStatementWithTransactionsByIdPagedAsync(
        Guid householdId, Guid creditCardId, Guid statementId, int page, int pageSize)
        => GetStatementPagedAsync(
            householdId, creditCardId,
            () => _statementRepository.GetByIdAsync(statementId),
            page, pageSize);

    private async Task<StatementWithTransactionsResponse?> GetStatementAsync(
        Guid householdId, Guid creditCardId, Func<Task<CreditCardStatement?>> loadStatement)
    {
        var resolved = await ResolveAsync(householdId, creditCardId, loadStatement);
        if (resolved is null)
            return null;

        var (card, statement) = resolved;

        var transactions = await _transactionQueryRepository.GetTransactionDetailsByStatementAsync(statement.Id);
        var netTotal = await _transactionQueryRepository.GetStatementNetTotalAsync(statement.Id);

        return new StatementWithTransactionsResponse(
            statement.Id,
            card.Id,
            card.Name,
            card.Limit,
            statement.DueDateFor(card.DueDay),
            netTotal,
            transactions
        );
    }

    private async Task<PaginatedStatementWithTransactionsResponse?> GetStatementPagedAsync(
        Guid householdId, Guid creditCardId, Func<Task<CreditCardStatement?>> loadStatement, int page, int pageSize)
    {
        if (page < 1)
            throw new DomainException("Page must be at least 1.");

        var resolved = await ResolveAsync(householdId, creditCardId, loadStatement);
        if (resolved is null)
            return null;

        var (card, statement) = resolved;

        var (transactions, totalCount) = await _transactionQueryRepository.GetTransactionDetailsByStatementPagedAsync(
            statement.Id, page, pageSize);
        var netTotal = await _transactionQueryRepository.GetStatementNetTotalAsync(statement.Id);

        return new PaginatedStatementWithTransactionsResponse(
            statement.Id,
            card.Id,
            card.Name,
            card.Limit,
            statement.DueDateFor(card.DueDay),
            netTotal,
            transactions,
            page,
            pageSize,
            totalCount
        );
    }

    /// <summary>
    /// Carrega o cartão do lar e a fatura que pertence a ele.
    /// Devolve null quando o cartão não é do lar ou a fatura não existe para o cartão.
    /// </summary>
    private async Task<ResolvedStatement?> ResolveAsync(
        Guid householdId, Guid creditCardId, Func<Task<CreditCardStatement?>> loadStatement)
    {
        var card = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (card is null || card.HouseholdId != householdId)
            return null;

        var statement = await loadStatement();
        if (statement is null || statement.CreditCardId != creditCardId)
            return null;

        return new ResolvedStatement(card, statement);
    }

    private sealed record ResolvedStatement(CreditCard Card, CreditCardStatement Statement);
}
