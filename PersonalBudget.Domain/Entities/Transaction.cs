public class Transaction
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }

    public Guid? CategoryId { get; private set; }
    public Guid? CreditCardId { get; private set; }

    public TransactionType Type { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public TransactionStatus Status { get; private set; }

    public Money Amount { get; private set; }
    public TransactionDate Date { get; private set; }
    public TransactionDescription Description { get; private set; }

    private Transaction(
        Guid userId,
        Guid accountId,
        Money amount,
        TransactionType type,
        PaymentMethod paymentMethod,
        TransactionDate date,
        TransactionDescription description,
        Guid? categoryId,
        Guid? creditCardId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Transaction must belong to a user.");

        if (accountId == Guid.Empty)
            throw new DomainException("Transaction must belong to an account.");

        if (paymentMethod == PaymentMethod.CreditCard && creditCardId is null)
            throw new DomainException("Credit card transaction must reference a credit card.");

        if (paymentMethod != PaymentMethod.CreditCard && creditCardId is not null)
            throw new DomainException("Only credit card transactions can reference a credit card.");

        Id = Guid.NewGuid();
        UserId = userId;
        AccountId = accountId;
        CategoryId = categoryId;
        CreditCardId = creditCardId;
        Amount = amount;
        Type = type;
        PaymentMethod = paymentMethod;
        Date = date;
        Description = description;
        Status = TransactionStatus.Pending;
    }

    protected Transaction() { }

    public static Transaction Create(
        Guid userId,
        Guid accountId,
        Money amount,
        TransactionType type,
        PaymentMethod paymentMethod,
        DateTime date,
        string description,
        Guid? categoryId = null,
        Guid? creditCardId = null)
    {
        return new Transaction(
            userId,
            accountId,
            amount,
            type,
            paymentMethod,
            new TransactionDate(date),
            new TransactionDescription(description),
            categoryId,
            creditCardId
        );
    }

    public void Complete()
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException("Only pending transactions can be completed.");

        Status = TransactionStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainException("Completed transactions cannot be cancelled.");

        Status = TransactionStatus.Cancelled;
    }
}
