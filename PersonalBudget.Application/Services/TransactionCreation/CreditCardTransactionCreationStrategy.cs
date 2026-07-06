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
        if (command.CreditCardId is null)
            throw new DomainException("CreditCardId é obrigatório para pagamentos com cartão de crédito.");

        var date = ParseDate(command.Date);

        var creditCard = await _creditCardRepository.GetByIdAsync(command.CreditCardId.Value);
        if (creditCard is null || creditCard.HouseholdId != command.HouseholdId)
            throw new DomainException("Cartão de crédito não encontrado.");

        var isInstallment = command.InstallmentCount is > 1;
        if (isInstallment && (!command.TotalAmount.HasValue || command.TotalAmount.Value <= 0))
            throw new DomainException("Parcelado exige TotalAmount quando InstallmentCount é maior que 1.");

        if (isInstallment)
        {
            return await CreateInstallmentsAsync(command, creditCard, date);
        }

        return await CreateSingleAsync(command, creditCard, date);
    }

    private async Task<Guid> CreateSingleAsync(
        CreateTransactionCommand command,
        CreditCard creditCard,
        DateTime date)
    {
        var statement = await GetOrCreateStatementAsync(creditCard, date, new Money(command.Amount), command.Type);

        var dueDate = ParseOptionalDueDate(command.DueDate);
        var initialStatus = command.Status ?? TransactionStatus.Pending;
        var transaction = Transaction.Create(
            command.UserId,
            command.HouseholdId,
            command.AttributionProfileId!.Value,
            creditCard.AccountId,
            new Money(command.Amount),
            command.Type,
            PaymentMethod.CreditCard,
            date,
            command.Description,
            command.CategoryId,
            command.CreditCardId,
            statement.Id,
            transferId: null,
            frequency: command.Frequency,
            expirationDate: null,
            dueDate: dueDate,
            initialStatus: initialStatus
        );

        await _transactionRepository.AddAsync(transaction);
        return transaction.Id;
    }

    private async Task<Guid> CreateInstallmentsAsync(
        CreateTransactionCommand command,
        CreditCard creditCard,
        DateTime firstDate)
    {
        var count = command.InstallmentCount!.Value;
        var totalAmount = command.TotalAmount!.Value;
        var amountPerInstallment = Math.Round(totalAmount / count, 2);
        var displayName = string.IsNullOrWhiteSpace(command.Title) ? command.Description : command.Title;
        var optionalFirstDue = ParseOptionalDueDate(command.DueDate);
        var recurrenceId = Guid.NewGuid();
        Guid firstTransactionId = default;

        for (var i = 0; i < count; i++)
        {
            var installmentDate = firstDate.AddMonths(i);
            var isLast = i == count - 1;
            var installmentAmount = isLast
                ? totalAmount - (amountPerInstallment * (count - 1))
                : amountPerInstallment;

            var statement = await GetOrCreateStatementAsync(creditCard, installmentDate, new Money(installmentAmount), command.Type);

            var description = $"{displayName} ({i + 1}/{count})";

            DateTime? dueForInstallment = optionalFirstDue.HasValue
                ? DateTime.SpecifyKind(optionalFirstDue.Value.AddMonths(i).Date, DateTimeKind.Utc)
                : null;

            var installmentInitialStatus = command.Status ?? TransactionStatus.Pending;
            var transaction = Transaction.Create(
                command.UserId,
                command.HouseholdId,
                command.AttributionProfileId!.Value,
                creditCard.AccountId,
                new Money(installmentAmount),
                command.Type,
                PaymentMethod.CreditCard,
                installmentDate,
                description,
                command.CategoryId,
                command.CreditCardId,
                statement.Id,
                transferId: null,
                frequency: TransactionFrequency.Installments,
                expirationDate: null,
                dueDate: dueForInstallment,
                initialStatus: installmentInitialStatus
            );

            transaction.AssignRecurrenceId(recurrenceId);
            await _transactionRepository.AddAsync(transaction);
            if (i == 0)
                firstTransactionId = transaction.Id;
        }

        return firstTransactionId;
    }

    private async Task<CreditCardStatement> GetOrCreateStatementAsync(
        CreditCard creditCard,
        DateTime date,
        Money amount,
        TransactionType transactionType)
    {
        var statement = await _creditCardStatementRepository.GetOpenStatementForDateAsync(creditCard.Id, date);

        if (statement is null)
        {
            var covering = await _creditCardStatementRepository.GetByCreditCardAndContainingDateAsync(creditCard.Id, date);
            if (covering is not null && covering.Status != BillStatus.Open)
                throw new DomainException("Não é possível lançar transações em fatura fechada ou já paga.");

            statement = CreditCardStatement.CreateForDate(creditCard.Id, date, creditCard.ClosingDay, creditCard.DueDay);
            statement.AddTransaction(amount, transactionType);
            await _creditCardStatementRepository.AddAsync(statement);
        }
        else
        {
            statement.AddTransaction(amount, transactionType);
            await _creditCardStatementRepository.UpdateAsync(statement);
        }

        return statement;
    }
}
