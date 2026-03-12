namespace PersonalBudget.Application.Services.TransactionCreation;

public class CreditCardTransactionCreationStrategy : TransactionCreationStrategyBase
{
    private readonly ICreditCardStatementRepository _creditCardStatementRepository;
    public CreditCardTransactionCreationStrategy(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICreditCardRepository creditCardRepository,
        ICreditCardStatementRepository creditCardStatementRepository)
        : base(transactionRepository, accountRepository, creditCardRepository)
    {
        _creditCardStatementRepository = creditCardStatementRepository;
    }

    public override PaymentMethod PaymentMethod => PaymentMethod.CreditCard;

    public override async Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
        if (command.Type is null)
            throw new DomainException("Transaction type is required.");

        if (command.CreditCardId is null)
            throw new DomainException("CreditCardId is required for credit card payments.");

        var date = ParseDate(command.Date);

        var creditCard = await _creditCardRepository.GetByIdAsync(command.CreditCardId.Value);

        if (creditCard is null || creditCard.UserId != command.UserId)
            throw new DomainException("Credit card not found.");

        var statement = await _creditCardStatementRepository.GetOpenStatementForDateAsync(command.CreditCardId.Value, date);

        if (statement is null)
        {
            statement = CreditCardStatement.CreateForDate(creditCard.Id, date, creditCard.ClosingDay, creditCard.DueDay);
            statement.AddTransaction(new Money(command.Amount));
            await _creditCardStatementRepository.AddAsync(statement);
        }
        else
        {
            statement.AddTransaction(new Money(command.Amount));
            await _creditCardStatementRepository.UpdateAsync(statement);
        }

        var transaction = Transaction.Create(
            command.UserId,
            creditCard.AccountId,
            new Money(command.Amount),
            command.Type.Value,
            PaymentMethod.CreditCard,
            date,
            command.Description,
            command.CategoryId,
            command.CreditCardId,
            statement.Id,
            transferId: null
        );

        await _transactionRepository.AddAsync(transaction);
        return transaction.Id;
    }
}
