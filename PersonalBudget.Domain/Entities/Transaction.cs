public class Transaction
{
    public Guid Id { get; }
    public Guid AccountId { get; }
    public Guid CategoryId { get; }
    public Guid? CreditCardId { get; }
    public Guid? InstallmentPlanId { get; }
    public PaymentMethod PaymentMethod { get; }
    public TransactionType Type { get; }
    public Money Amount { get; }
    public DateTime OccurredAt { get; }
    public TransactionStatus Status { get; private set; }
    public string Description { get; }

    public Transaction(
        Guid accountId,
        Guid categoryId,
        Money amount,
        TransactionType type,
        PaymentMethod paymentMethod,
        DateTime occurredAt,
        Guid? creditCardId = null,
        Guid? installmentPlanId = null,
        string description = "")
    {
        if (accountId == Guid.Empty)
            throw new DomainException("Transaction must have an account.");

        if (categoryId == Guid.Empty)
            throw new DomainException("Transaction must have a category.");

        if (paymentMethod == PaymentMethod.CreditCard && creditCardId == null)
            throw new DomainException("Credit card transactions must reference a credit card.");

        if (paymentMethod != PaymentMethod.CreditCard && creditCardId != null)
            throw new DomainException("Only credit card transactions can reference a credit card.");

        Id = Guid.NewGuid();
        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        Type = type;
        PaymentMethod = paymentMethod;
        CreditCardId = creditCardId;
        InstallmentPlanId = installmentPlanId;
        OccurredAt = occurredAt;
        Description = description;
        Status = TransactionStatus.Pending;
    }

    protected Transaction() { }

    public void MarkAsCompleted()
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainException("Transaction is already completed.");

        Status = TransactionStatus.Completed;
    }

    public void MarkAsSimulated()
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainException("Completed transaction cannot be simulated.");

        Status = TransactionStatus.Simulated;
    }

    public bool IsCompleted()
        => Status == TransactionStatus.Completed;
}