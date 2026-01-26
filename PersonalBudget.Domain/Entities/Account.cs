public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Bank Bank { get; private set; }
    public BankAgency Agency { get; private set; } = null!;
    public BankAccountNumber Number { get; private set; } = null!;
    public Money Balance { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public Account(
        Guid userId,
        Bank bank,
        BankAgency agency,
        BankAccountNumber number,
        Money initialBalance)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Account must belong to a user.");

        Id = Guid.NewGuid();
        UserId = userId;
        Bank = bank;
        Agency = agency;
        Number = number;
        Balance = initialBalance;
        CreatedAt = DateTime.UtcNow;
    }

    public void Credit(Money amount)
    {
        Balance = Balance.Add(amount);
    }

    public void Debit(Money amount)
    {
        Balance = Balance.Subtract(amount);
    }
}