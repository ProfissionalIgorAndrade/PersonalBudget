public sealed class TransactionDate
{
    public DateTime Value { get; }

    public TransactionDate(DateTime value)
    {
        if (value == default)
            throw new DomainException("Transaction date is invalid.");

        Value = value.Date;
    }

    public override bool Equals(object? obj)
        => obj is TransactionDate other && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();
}
