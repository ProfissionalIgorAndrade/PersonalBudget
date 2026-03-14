public sealed class Money
{
    public decimal Amount { get; private set; }

    // ✅ Construtor de domínio (uso normal)
    public Money(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("O valor não pode ser negativo.");

        Amount = amount;
    }

    // ✅ Construtor para EF Core (OBRIGATÓRIO)
    private Money()
    {
        Amount = 0;
    }

    public Money Add(Money other)
        => new Money(Amount + other.Amount);

    public Money Subtract(Money other)
    {
        if (other.Amount > Amount)
            throw new DomainException("Não é possível subtrair mais do que o valor disponível.");

        return new Money(Amount - other.Amount);
    }

    public bool IsZero() => Amount == 0;
    public bool IsNegative() => Amount < 0;

    public override bool Equals(object? obj)
        => obj is Money other && Amount == other.Amount;

    public override int GetHashCode()
        => Amount.GetHashCode();
}
