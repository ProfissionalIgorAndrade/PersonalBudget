public sealed class Money
{
    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Money amount must be greater than zero.");

        Amount = amount;
    }

    public Money Add(Money other)
    {
        return new Money(Amount + other.Amount);
    }

    public Money Subtract(Money other)
    {
        if (other.Amount > Amount)
            throw new DomainException("Cannot subtract more money than available.");

        return new Money(Amount - other.Amount);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Money other)
            return false;

        return Amount == other.Amount;
    }

    public override int GetHashCode()
        => Amount.GetHashCode();
}
