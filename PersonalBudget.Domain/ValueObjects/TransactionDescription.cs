public sealed class TransactionDescription
{
    public string Value { get; }

    public TransactionDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Descrição da transação é obrigatória.");

        if (value.Length > 200)
            throw new DomainException("Descrição da transação é muito longa.");

        Value = value.Trim();
    }

    public override bool Equals(object? obj)
        => obj is TransactionDescription other && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();
}
