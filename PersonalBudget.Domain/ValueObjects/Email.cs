public sealed class Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email is required.");

        if (!value.Contains("@"))
            throw new DomainException("Invalid email format.");

        Value = value.ToLowerInvariant();
    }

    public override bool Equals(object? obj)
        => obj is Email other && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();
}