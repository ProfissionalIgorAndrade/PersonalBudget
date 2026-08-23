namespace PersonalBudget.Application.Services.TransactionCreation;

public class TransferTransactionCreationStrategy : TransactionCreationStrategyBase
{
    private readonly ICategoryRepository _categoryRepository;

    public TransferTransactionCreationStrategy(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICreditCardRepository creditCardRepository,
        ICategoryRepository categoryRepository)
        : base(transactionRepository, accountRepository, creditCardRepository)
    {
        _categoryRepository = categoryRepository;
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
        var dueDate = ParseOptionalDueDate(command.DueDate);
        var transferId = Guid.NewGuid();
        var transferCategoryId = await GetOrCreateTransferCategoryIdAsync(command.HouseholdId);

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
            categoryId: transferCategoryId,
            creditCardId: null,
            transferId: transferId,
            frequency: TransactionFrequency.Variable,
            expirationDate: null,
            dueDate: dueDate,
            observations: command.Observations);

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
            categoryId: transferCategoryId,
            creditCardId: null,
            transferId: transferId,
            frequency: TransactionFrequency.Variable,
            expirationDate: null,
            dueDate: dueDate,
            observations: command.Observations);

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

    private async Task<Guid> GetOrCreateTransferCategoryIdAsync(Guid householdId)
    {
        var categories = await _categoryRepository.GetByHouseholdAsync(householdId);
        var existing = categories.FirstOrDefault(c => c.IsSystem && c.Name == "Transferência");
        if (existing is not null)
            return existing.Id;

        var newCategory = new Category(householdId, "Transferência", isSystem: true, CategoryType.Expense);
        await _categoryRepository.AddAsync(newCategory);
        return newCategory.Id;
    }
}
