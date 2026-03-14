
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
        if (command.Type is null)
            throw new DomainException("Tipo da transação é obrigatório.");

        var accountId = command.AccountId
            ?? throw new DomainException("AccountId é obrigatório para o método de pagamento Conta.");

        var account = await GetAccountOrThrowAsync(accountId, command.UserId);
        var date = ParseDate(command.Date);

        var transaction = Transaction.Create(
            command.UserId,
            accountId,
            new Money(command.Amount),
            command.Type.Value,
            PaymentMethod.Account,
            date,
            command.Description,
            command.CategoryId,
            creditCardId: null,
            transferId: null
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
