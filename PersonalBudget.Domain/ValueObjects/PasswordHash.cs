public sealed class PasswordHash
{
    public string Value { get; }

    public PasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Hash da senha é obrigatório.");

        Value = value;
    }

    public override bool Equals(object? obj)
        => obj is PasswordHash other && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();
}
