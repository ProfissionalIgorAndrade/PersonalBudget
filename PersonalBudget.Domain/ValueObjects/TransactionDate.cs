public sealed class TransactionDate
{
    public DateTime Value { get; }

    public TransactionDate(DateTime value)
    {
        if (value == default)
            throw new DomainException("Data da transação é inválida.");

        Value = DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    public override bool Equals(object? obj)
        => obj is TransactionDate other && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();
}
