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
            throw new DomainException($"A estratégia de transferência aceita apenas PaymentMethod.Transfer. Recebido: {command.PaymentMethod}. Verifique se a estratégia correta está selecionada no TransactionService.");

        if (command.FromAccountId is null || command.ToAccountId is null)
            throw new DomainException("FromAccountId e ToAccountId são obrigatórios para transferências.");

        if (command.FromAccountId == command.ToAccountId)
            throw new DomainException("Conta de origem e destino devem ser diferentes.");

        var fromAccount = await GetAccountOrThrowAsync(command.FromAccountId.Value, command.HouseholdId);
        var toAccount = await GetAccountOrThrowAsync(command.ToAccountId.Value, command.HouseholdId);

        var date = ParseDate(command.Date);
        var transferId = Guid.NewGuid();

        var outTx = Transaction.Create(
            command.UserId,
            command.HouseholdId,
            command.AttributionProfileId!.Value,
            command.FromAccountId.Value,
            new Money(command.Amount),
            TransactionType.Expense,
            PaymentMethod.Transfer,
            date,
            command.Description,
            categoryId: null,
            creditCardId: null,
            transferId: transferId,
            frequency: TransactionFrequency.Variable,
            expirationDate: null);

        var inTx = Transaction.Create(
            command.UserId,
            command.HouseholdId,
            command.AttributionProfileId!.Value,
            command.ToAccountId.Value,
            new Money(command.Amount),
            TransactionType.Income,
            PaymentMethod.Transfer,
            date,
            command.Description,
            categoryId: null,
            creditCardId: null,
            transferId: transferId,
            frequency: TransactionFrequency.Variable,
            expirationDate: null);

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