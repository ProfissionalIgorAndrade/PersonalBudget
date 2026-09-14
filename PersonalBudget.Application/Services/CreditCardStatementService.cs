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

        var transactions = await _transactionQueryRepository.GetTransactionDetailsByStatementAsync(statement.Id);
        var netTotal = await _transactionQueryRepository.GetStatementNetTotalAsync(statement.Id);
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

    public async Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsByIdAsync(Guid householdId, Guid creditCardId, Guid statementId)
    {
        var card = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (card is null || card.HouseholdId != householdId)
            return null;

        var statement = await _statementRepository.GetByIdAsync(statementId);
        if (statement is null || statement.CreditCardId != creditCardId)
            return null;

        var transactions = await _transactionQueryRepository.GetTransactionDetailsByStatementAsync(statement.Id);
        var netTotal = await _transactionQueryRepository.GetStatementNetTotalAsync(statement.Id);
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
            statement.Id, page, pageSize);
        var netTotal = await _transactionQueryRepository.GetStatementNetTotalAsync(statement.Id);
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

    public async Task<PaginatedStatementWithTransactionsResponse?> GetStatementWithTransactionsByIdPagedAsync(
        Guid householdId, Guid creditCardId, Guid statementId, int page, int pageSize)
    {
        if (page < 1)
            throw new DomainException("Page must be at least 1.");

        var card = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (card is null || card.HouseholdId != householdId)
            return null;

        var statement = await _statementRepository.GetByIdAsync(statementId);
        if (statement is null || statement.CreditCardId != creditCardId)
            return null;

        var (transactions, totalCount) = await _transactionQueryRepository.GetTransactionDetailsByStatementPagedAsync(
            statement.Id, page, pageSize);
        var netTotal = await _transactionQueryRepository.GetStatementNetTotalAsync(statement.Id);
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
    /// Estorna o pagamento de uma fatura paga: reverte as transações para pendente
    /// e muda o status para Fechada (Paid → Closed).
    /// Quando PaidFromAccountId está registrado, também credita o valor na conta de origem
    /// e cria um lançamento de estorno visível.
    /// </summary>
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

}