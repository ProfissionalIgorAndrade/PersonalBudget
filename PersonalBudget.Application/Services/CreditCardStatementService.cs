using PersonalBudget.Application.DTOs.CreditCard;
using PersonalBudget.Application.Interfaces;

public class CreditCardStatementService : ICreditCardStatementService
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICreditCardStatementRepository _statementRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionQueryRepository _transactionQueryRepository;

    public CreditCardStatementService(
        ICreditCardRepository creditCardRepository,
        ICreditCardStatementRepository statementRepository,
        IAccountRepository accountRepository,
        ITransactionQueryRepository transactionQueryRepository)
    {
        _creditCardRepository = creditCardRepository;
        _statementRepository = statementRepository;
        _accountRepository = accountRepository;
        _transactionQueryRepository = transactionQueryRepository;
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

    public async Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsAsync(Guid userId, Guid creditCardId, int month, int year)
    {
        var card = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (card is null || card.UserId != userId)
            return null;

        var statement = await _statementRepository.GetByCreditCardAndClosingMonthYearAsync(creditCardId, month, year);
        if (statement is null)
            return null;

        var transactions = await _transactionQueryRepository.GetTransactionDetailsByStatementIdAsync(statement.Id);
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
            statement.TotalAmount.Amount,
            transactions
        );
    }

    /// <summary>
    /// Calcula a data de vencimento: se dia de vencimento >= dia de fechamento, vence no mesmo mês;
    /// caso contrário, vence no mês seguinte (ex.: fechamento 30/mar, vencimento dia 8 → 8/abr).
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
        return new DateTime(dueYear, dueMonth, day);
    }

    public async Task CloseAsync(CloseStatementCommand command)
    {
        var card = await _creditCardRepository.GetByIdWithStatementsAsync(command.CreditCardId);

        if (card is null)
            throw new ApplicationException("Cartão não encontrado.");

        if (card.UserId != command.UserId)
            throw new ApplicationException("Cartão não pertence ao usuário.");

        card.CloseStatement(command.StatementId);

        await _creditCardRepository.SaveChangesAsync();
    }

    public async Task PayAsync(PayStatementCommand command)
    {
        var card = await _creditCardRepository.GetByIdWithStatementsAsync(command.CreditCardId);

        if (card is null)
            throw new ApplicationException("Cartão não encontrado.");

        if (card.UserId != command.UserId)
            throw new ApplicationException("Cartão não pertence ao usuário.");

        var statement = await _statementRepository.GetByIdAsync(command.StatementId);

        if (statement is null)
            throw new ApplicationException("Fatura não encontrada.");

        var account = await _accountRepository.GetByIdAsync(card.AccountId);
        if (account is null)
            throw new ApplicationException("Conta não encontrada.");

        var amount = card.PayStatement(statement.Id);

        account.Debit(new Money(amount));

        await _accountRepository.SaveChangesAsync();
        await _statementRepository.SaveChangesAsync();
    }
}