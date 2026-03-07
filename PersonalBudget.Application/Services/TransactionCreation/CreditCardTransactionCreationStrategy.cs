namespace PersonalBudget.Application.Services.TransactionCreation;

public class CreditCardTransactionCreationStrategy : TransactionCreationStrategyBase
{
    public CreditCardTransactionCreationStrategy(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICreditCardRepository creditCardRepository)
        : base(transactionRepository, accountRepository, creditCardRepository)
    {
    }

    public override PaymentMethod PaymentMethod => PaymentMethod.CreditCard;

    public override async Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
        if (command.Type is null)
            throw new DomainException("Transaction type is required.");

        if (command.CreditCardId is null)
            throw new DomainException("CreditCardId is required for credit card payments.");

        // Resolve account via AccountId (se veio) ou via cartão
        Guid accountId;
        if (command.AccountId is { } accId)
        {
            accountId = accId;
        }
        else
        {
            var creditCard = await _creditCardRepository.GetByIdAsync(command.CreditCardId.Value);
            if (creditCard is null || creditCard.UserId != command.UserId)
                throw new DomainException("Credit card not found.");
            accountId = creditCard.AccountId;
        }

        var account = await GetAccountOrThrowAsync(accountId, command.UserId);
        var date = ParseDate(command.Date);

        var transaction = Transaction.Create(
            command.UserId,
            accountId,
            new Money(command.Amount),
            command.Type.Value,
            PaymentMethod.CreditCard,
            date,
            command.Description,
            command.CategoryId,
            command.CreditCardId,
            transferId: null
        );

        await _transactionRepository.AddAsync(transaction);
        return transaction.Id;
    }
}
