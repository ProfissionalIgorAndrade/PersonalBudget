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

        var isInstallment = command.InstallmentCount is > 1;
        if (isInstallment && (!command.TotalAmount.HasValue || command.TotalAmount.Value <= 0))
            throw new DomainException("Parcelado requires TotalAmount when InstallmentCount is greater than 1.");

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
        var statement = await GetOrCreateStatementAsync(creditCard, date, new Money(command.Amount));

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

    private async Task<Guid> CreateInstallmentsAsync(
        CreateTransactionCommand command,
        CreditCard creditCard,
        DateTime firstDate)
    {
        var count = command.InstallmentCount!.Value;
        var totalAmount = command.TotalAmount!.Value;
        var amountPerInstallment = Math.Round(totalAmount / count, 2);
        var displayName = string.IsNullOrWhiteSpace(command.Title) ? command.Description : command.Title;
        Guid firstTransactionId = default;

        for (var i = 0; i < count; i++)
        {
            var installmentDate = firstDate.AddMonths(i);
            var isLast = i == count - 1;
            var installmentAmount = isLast
                ? totalAmount - (amountPerInstallment * (count - 1))
                : amountPerInstallment;

            var statement = await GetOrCreateStatementAsync(creditCard, installmentDate, new Money(installmentAmount));

            var description = $"{displayName} ({i + 1}/{count})";

            var transaction = Transaction.Create(
                command.UserId,
                creditCard.AccountId,
                new Money(installmentAmount),
                command.Type!.Value,
                PaymentMethod.CreditCard,
                installmentDate,
                description,
                command.CategoryId,
                command.CreditCardId,
                statement.Id,
                transferId: null
            );

            await _transactionRepository.AddAsync(transaction);
            if (i == 0)
                firstTransactionId = transaction.Id;
        }

        return firstTransactionId;
    }

    private async Task<CreditCardStatement> GetOrCreateStatementAsync(
        CreditCard creditCard,
        DateTime date,
        Money amount)
    {
        var statement = await _creditCardStatementRepository.GetOpenStatementForDateAsync(creditCard.Id, date);

        if (statement is null)
        {
            statement = CreditCardStatement.CreateForDate(creditCard.Id, date, creditCard.ClosingDay, creditCard.DueDay);
            statement.AddTransaction(amount);
            await _creditCardStatementRepository.AddAsync(statement);
        }
        else
        {
            statement.AddTransaction(amount);
            await _creditCardStatementRepository.UpdateAsync(statement);
        }

        return statement;
    }
}
