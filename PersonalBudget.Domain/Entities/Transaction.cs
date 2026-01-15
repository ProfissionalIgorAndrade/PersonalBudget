public class Transaction
{

    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }

    public Transaction(Guid accountId, decimal amount, DateTime date, TransactionType type, TransactionStatus status)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Amount = amount;
        Date = date;
        Type = type;
        Status = status;
    }

    public bool IsCompleted()
        => Status == TransactionStatus.Completed;

    public bool IsPending()
        => Status == TransactionStatus.Pending;
}