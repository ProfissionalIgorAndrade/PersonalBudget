public class RecurringRule
{
    public RecurringRule(
        Guid accountId,
        Guid categoryId,
        Money amount,
        TransactionType type,
        PaymentMethod paymentMethod,
        RecurrenceFrequency frequency,
        DateTime startDate,
        string description,
        DateTime? endDate = null)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("Recurring rule must have an account.");

        if (categoryId == Guid.Empty)
            throw new DomainException("Recurring rule must have a category.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Recurring rule description is required.");

        if (endDate.HasValue && endDate <= startDate)
            throw new DomainException("End date must be after start date.");

        if (paymentMethod == PaymentMethod.CreditCard)
            throw new DomainException("Credit card recurrence must reference a credit card.");

        Id = Guid.NewGuid();
        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        Type = type;
        PaymentMethod = paymentMethod;
        Frequency = frequency;
        StartDate = startDate;
        EndDate = endDate;
        Description = description;
        IsCancelled = false;
    }

    public Guid Id { get; }
    public Guid AccountId { get; }
    public Guid CategoryId { get; }
    public Guid? CreditCardId { get; }
    public TransactionType Type { get; }
    public PaymentMethod PaymentMethod { get; }
    public Money Amount { get; }
    public RecurrenceFrequency Frequency { get; }
    public DateTime StartDate { get; }
    public DateTime? EndDate { get; }
    public string Description { get; }
    public bool IsCancelled { get; private set; }

    public void Cancel()
    {
        IsCancelled = true;
    }

    public IEnumerable<Transaction> GenerateTransactions(DateTime until)
    {
        if (IsCancelled)
            throw new DomainException("Cancelled recurring rule cannot generate transactions.");

        if (until < StartDate)
            throw new DomainException("Generation date must be after start date.");

        var transactions = new List<Transaction>();
        var currentDate = StartDate;

        while (currentDate <= until)
        {
            if (EndDate.HasValue && currentDate > EndDate.Value)
                break;

            transactions.Add(
                new Transaction(
                    accountId: AccountId,
                    categoryId: CategoryId,
                    amount: Amount,
                    type: Type,
                    paymentMethod: PaymentMethod,
                    occurredAt: currentDate,
                    description: Description,
                    creditCardId: CreditCardId
                )
            );

            currentDate = AddFrequency(currentDate);
        }

        return transactions;
    }

    private DateTime AddFrequency(DateTime date)
    {
        return Frequency switch
        {
            RecurrenceFrequency.Monthly => date.AddMonths(1),
            RecurrenceFrequency.Quarterly => date.AddMonths(3),
            RecurrenceFrequency.SemiAnnual => date.AddMonths(6),
            RecurrenceFrequency.Annual => date.AddYears(1),
            _ => throw new DomainException("Invalid recurrence frequency.")
        };
    }
}