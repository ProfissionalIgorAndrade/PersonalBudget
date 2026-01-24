public class Account
{
    public Account(string name, Money balance)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Account name cannot be empty.");

        Id = Guid.NewGuid();
        Name = name;
        Balance = balance;
        CreatedAt = DateTime.Now;
    }

    protected Account()
    {
        // EF Core only
        Balance = new Money(0);
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Money Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void Credit(Money amount)
    {
        Balance = Balance.Add(amount);
    }

    public void Debit(Money amount)
    {
        Balance = Balance.Subtract(amount);
    }
}