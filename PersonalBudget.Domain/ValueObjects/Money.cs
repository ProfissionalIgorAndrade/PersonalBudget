public sealed class Money
{
    public decimal Amount { get; private set; }

    // ✅ Construtor de domínio (uso normal)
    public Money(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");

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
            throw new DomainException("Cannot subtract more money than available.");

        return new Money(Amount - other.Amount);
    }

    public bool IsZero() => Amount == 0;
    public bool IsNegative() => Amount < 0;

    public override bool Equals(object? obj)
        => obj is Money other && Amount == other.Amount;

    public override int GetHashCode()
        => Amount.GetHashCode();
}
