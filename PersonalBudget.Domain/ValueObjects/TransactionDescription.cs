public sealed class TransactionDescription
{
    public string Value { get; }

    public TransactionDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Transaction description is required.");

        if (value.Length > 200)
            throw new DomainException("Transaction description is too long.");

        Value = value.Trim();
    }

    public override bool Equals(object? obj)
        => obj is TransactionDescription other && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();
}
