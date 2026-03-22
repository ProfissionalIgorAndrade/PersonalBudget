public class Account
{
    public Guid Id { get; private set; }
    /// <summary>Usuário que criou a conta (auditoria).</summary>
    public Guid UserId { get; private set; }
    /// <summary>Lar ao qual a conta pertence (visível a todos os membros).</summary>
    public Guid HouseholdId { get; private set; }
    public Bank Bank { get; private set; }
    public BankAgency Agency { get; private set; } = null!;
    public BankAccountNumber Number { get; private set; } = null!;
    public Money Balance { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Account(
        Guid userId,
        Guid householdId,
        Bank bank,
        BankAgency agency,
        BankAccountNumber number,
        Money initialBalance)
    {
        if (userId == Guid.Empty)
            throw new DomainException("A conta deve pertencer a um usuário.");
        if (householdId == Guid.Empty)
            throw new DomainException("A conta deve pertencer a um lar.");

        Id = Guid.NewGuid();
        UserId = userId;
        HouseholdId = householdId;
        Bank = bank;
        Agency = agency;
        Number = number;
        Balance = initialBalance;
        CreatedAt = DateTime.UtcNow;
    }

    protected Account() { }

    public static Account Create(
       Guid userId,
       Guid householdId,
       Bank bank,
       BankAgency agency,
       BankAccountNumber number,
       Money initialBalance)
    {
        return new Account(userId, householdId, bank, agency, number, initialBalance);
    }

    public void UpdateBankInfo(
        Bank bank,
        BankAgency agency,
        BankAccountNumber number)
    {
        if (!IsActive)
            throw new DomainException("Conta inativa não pode ser atualizada.");

        Bank = bank;
        Agency = agency;
        Number = number;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("A conta já está inativa.");

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