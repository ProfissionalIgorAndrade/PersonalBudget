public class Transaction
{
    public Guid Id { get; }
    public Guid AccountId { get; }
    public Guid CategoryId { get; }
    public TransactionType Type { get; }
    public Money Amount { get; }
    public DateTime OccurredAt { get; }
    public TransactionStatus Status { get; private set; }

    public Transaction(
        Guid accountId,
        Guid categoryId,
        Money amount,
        TransactionType type,
        TransactionStatus status,
        DateTime occurredAt)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("Transaction must have an account.");

        if (categoryId == Guid.Empty)
            throw new DomainException("Transaction must have a category.");

        Id = Guid.NewGuid();
        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        Type = type;
        Status = status;
        OccurredAt = occurredAt;
    }

    public void MarkAsCompleted()
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainException("Transaction is already completed.");

        Status = TransactionStatus.Completed;
    }

    public bool IsCompleted()
        => Status == TransactionStatus.Completed;
}