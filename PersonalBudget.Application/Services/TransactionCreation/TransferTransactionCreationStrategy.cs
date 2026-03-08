namespace PersonalBudget.Application.Services.TransactionCreation;

public class TransferTransactionCreationStrategy : TransactionCreationStrategyBase
{
    public TransferTransactionCreationStrategy(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICreditCardRepository creditCardRepository)
        : base(transactionRepository, accountRepository, creditCardRepository)
    {
    }

    public override PaymentMethod PaymentMethod => PaymentMethod.Transfer;

    public override async Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
        if (command.PaymentMethod != PaymentMethod.Transfer)
            throw new DomainException($"Transfer strategy only accepts PaymentMethod.Transfer. Received: {command.PaymentMethod}. Check that the correct strategy is selected by TransactionService.");

        if (command.FromAccountId is null || command.ToAccountId is null)
            throw new DomainException("FromAccountId and ToAccountId are required for transfers.");

        if (command.FromAccountId == command.ToAccountId)
            throw new DomainException("Origin and destination accounts must be different.");

        var fromAccount = await GetAccountOrThrowAsync(command.FromAccountId.Value, command.UserId);
        var toAccount = await GetAccountOrThrowAsync(command.ToAccountId.Value, command.UserId);

        var date = ParseDate(command.Date);
        var transferId = Guid.NewGuid();

        var outTx = Transaction.Create(
            command.UserId,
            command.FromAccountId.Value,
            new Money(command.Amount),
            TransactionType.Expense,
            PaymentMethod.Account,
            date,
            command.Description,
            categoryId: null,
            creditCardId: null,
            transferId: transferId);

        var inTx = Transaction.Create(
            command.UserId,
            command.ToAccountId.Value,
            new Money(command.Amount),
            TransactionType.Income,
            PaymentMethod.Account,
            date,
            command.Description,
            categoryId: null,
            creditCardId: null,
            transferId: transferId);

        outTx.Complete();
        inTx.Complete();
        TransactionApplier.Apply(fromAccount, outTx);
        TransactionApplier.Apply(toAccount, inTx);

        await _transactionRepository.AddAsync(outTx);
        await _transactionRepository.AddAsync(inTx);
        await _accountRepository.UpdateAsync(fromAccount);
        await _accountRepository.UpdateAsync(toAccount);

        return transferId;
    }
}