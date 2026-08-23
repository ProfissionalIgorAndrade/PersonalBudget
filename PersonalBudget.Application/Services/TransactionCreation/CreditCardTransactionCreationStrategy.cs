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

        if (command.StatementMonth is null || command.StatementYear is null)
            throw new DomainException("StatementMonth e StatementYear são obrigatórios para pagamentos com cartão de crédito.");

        if (command.StatementMonth < 1 || command.StatementMonth > 12)
            throw new DomainException("StatementMonth deve estar entre 1 e 12.");

        var date = ParseDate(command.Date);

        var creditCard = await _creditCardRepository.GetByIdAsync(command.CreditCardId.Value);
        if (creditCard is null || creditCard.HouseholdId != command.HouseholdId)
            throw new DomainException("Cartão de crédito não encontrado.");

        var isInstallment = command.InstallmentCount is > 1;
        if (isInstallment && (!command.TotalAmount.HasValue || command.TotalAmount.Value <= 0))
            throw new DomainException("Parcelado exige TotalAmount quando InstallmentCount é maior que 1.");

        if (isInstallment)
            return await CreateInstallmentsAsync(command, creditCard, date);

        return await CreateSingleAsync(command, creditCard, date);
    }

    private async Task<Guid> CreateSingleAsync(
        CreateTransactionCommand command,
        CreditCard creditCard,
        DateTime date)
    {
        var statement = await GetOrCreateStatementForMonthAsync(
            creditCard, command.StatementMonth!.Value, command.StatementYear!.Value,
            new Money(command.Amount), command.Type);

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
            initialStatus: initialStatus,
            observations: command.Observations
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

        var firstMonth = command.StatementMonth!.Value;
        var firstYear = command.StatementYear!.Value;

        for (var i = 0; i < count; i++)
        {
            // Avança mês/ano mantendo lógica calendárica correta
            var totalMonths = (firstYear * 12 + firstMonth - 1) + i;
            var installmentMonth = (totalMonths % 12) + 1;
            var installmentYear = totalMonths / 12;

            var installmentDate = firstDate.AddMonths(i);
            var isLast = i == count - 1;
            var installmentAmount = isLast
                ? totalAmount - (amountPerInstallment * (count - 1))
                : amountPerInstallment;

            var statement = await GetOrCreateStatementForMonthAsync(
                creditCard, installmentMonth, installmentYear,
                new Money(installmentAmount), command.Type);

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
                initialStatus: installmentInitialStatus,
                observations: command.Observations
            );

            transaction.AssignRecurrenceId(recurrenceId);
            await _transactionRepository.AddAsync(transaction);
            if (i == 0)
                firstTransactionId = transaction.Id;
        }

        return firstTransactionId;
    }

    private async Task<CreditCardStatement> GetOrCreateStatementForMonthAsync(
        CreditCard creditCard,
        int month,
        int year,
        Money amount,
        TransactionType transactionType)
    {
        var statement = await _creditCardStatementRepository.GetByCreditCardAndClosingMonthYearAsync(creditCard.Id, month, year);

        if (statement is null)
        {
            statement = CreditCardStatement.CreateForMonth(creditCard.Id, month, year, creditCard.ClosingDay, creditCard.DueDay);
            statement.AddTransaction(amount, transactionType);
            await _creditCardStatementRepository.AddAsync(statement);
        }
        else
        {
            // AddTransaction já lança exceção se a fatura não estiver Open
            statement.AddTransaction(amount, transactionType);
            await _creditCardStatementRepository.UpdateAsync(statement);
        }

        return statement;
    }
}
