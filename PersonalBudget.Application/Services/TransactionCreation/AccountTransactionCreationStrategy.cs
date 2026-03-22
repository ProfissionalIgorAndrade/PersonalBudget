
namespace PersonalBudget.Application.Services.TransactionCreation;

public class AccountTransactionCreationStrategy : TransactionCreationStrategyBase
{
    public AccountTransactionCreationStrategy(
       ITransactionRepository transactionRepository,
       IAccountRepository accountRepository,
       ICreditCardRepository creditCardRepository)
       : base(transactionRepository, accountRepository, creditCardRepository)
    {
    }

    public override PaymentMethod PaymentMethod => PaymentMethod.Account;

    public override async Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
        var accountId = command.AccountId
            ?? throw new DomainException("AccountId é obrigatório para o método de pagamento Conta.");

        var account = await GetAccountOrThrowAsync(accountId, command.HouseholdId);
        var date = ParseDate(command.Date);

        var expiration = ParseOptionalExpirationDate(command.ExpirationDate);

        var transaction = Transaction.Create(
            command.UserId,
            command.HouseholdId,
            command.AttributionProfileId!.Value,
            accountId,
            new Money(command.Amount),
            command.Type,
            PaymentMethod.Account,
            date,
            command.Description,
            command.CategoryId,
            creditCardId: null,
            transferId: null,
            frequency: command.Frequency,
            expirationDate: expiration
        );

        if (command.AutoComplete)
        {
            transaction.Complete();
            TransactionApplier.Apply(account, transaction);
            await _accountRepository.UpdateAsync(account);
        }

        await _transactionRepository.AddAsync(transaction);
        return transaction.Id;
    }

}
