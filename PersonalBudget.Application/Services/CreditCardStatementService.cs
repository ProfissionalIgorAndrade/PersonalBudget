using PersonalBudget.Application.DTOs.CreditCard;
using PersonalBudget.Application.Interfaces;

public class CreditCardStatementService : ICreditCardStatementService
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICreditCardStatementRepository _statementRepository;
    private readonly IAccountRepository _accountRepository;

    public CreditCardStatementService(
        ICreditCardRepository creditCardRepository,
        ICreditCardStatementRepository statementRepository,
        IAccountRepository accountRepository)
    {
        _creditCardRepository = creditCardRepository;
        _statementRepository = statementRepository;
        _accountRepository = accountRepository;
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

    public async Task CloseAsync(CloseStatementCommand command)
    {
        var card = await _creditCardRepository.GetByIdAsync(command.CreditCardId);

        if (card is null)
            throw new ApplicationException("Credit card not found.");

        card.CloseStatement(command.StatementId);

        await _creditCardRepository.SaveChangesAsync();
    }

    public async Task PayAsync(PayStatementCommand command)
    {
        var statement = await _statementRepository.GetByIdAsync(command.StatementId);

        if (statement is null)
            throw new ApplicationException("Statement not found.");

        var card = await _creditCardRepository.GetByIdAsync(statement.CreditCardId);

        var account = await _accountRepository.GetByIdAsync(card.AccountId);

        var amount = card.PayStatement(statement.Id);

        account.Debit(new Money(amount));

        await _accountRepository.SaveChangesAsync();
        await _statementRepository.SaveChangesAsync();
    }
}