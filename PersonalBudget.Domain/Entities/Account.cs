public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Bank Bank { get; private set; }
    public BankAgency Agency { get; private set; } = null!;
    public BankAccountNumber Number { get; private set; } = null!;
    public Money Balance { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    protected Account() { }

    public static Account Create(
       Guid userId,
       Bank bank,
       BankAgency agency,
       BankAccountNumber number,
       Money initialBalance)
    {
        return new Account(userId, bank, agency, number, initialBalance);
    }

    public void UpdateBankInfo(
        Bank bank,
        BankAgency agency,
        BankAccountNumber number)
    {
        if (!IsActive)
            throw new DomainException("Inactive account cannot be updated.");

        Bank = bank;
        Agency = agency;
        Number = number;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Account is already inactive.");

        IsActive = false;
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