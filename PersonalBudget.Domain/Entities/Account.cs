public class Account
{
    public Guid Id { get; private set; }
    /// <summary>Usuário que criou a conta (auditoria).</summary>
    public Guid UserId { get; private set; }
    /// <summary>Lar ao qual a conta pertence (visível a todos os membros).</summary>
    public Guid HouseholdId { get; private set; }
    /// <summary>Membro da família ao qual esta conta pertence.</summary>
    public Guid? MemberProfileId { get; private set; }
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
        Money initialBalance,
        Guid memberProfileId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("A conta deve pertencer a um usuário.");
        if (householdId == Guid.Empty)
            throw new DomainException("A conta deve pertencer a um lar.");
        if (memberProfileId == Guid.Empty)
            throw new DomainException("A conta deve estar vinculada a um membro da família.");

        Id = Guid.NewGuid();
        UserId = userId;
        HouseholdId = householdId;
        MemberProfileId = memberProfileId;
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
       Money initialBalance,
       Guid memberProfileId)
    {
        return new Account(userId, householdId, bank, agency, number, initialBalance, memberProfileId);
    }

    public void UpdateBankInfo(
        Bank bank,
        BankAgency agency,
        BankAccountNumber number,
        Guid? memberProfileId = null)
    {
        if (!IsActive)
            throw new DomainException("Conta inativa não pode ser atualizada.");
        if (memberProfileId == Guid.Empty)
            throw new DomainException("A conta deve estar vinculada a um membro da família.");

        Bank = bank;
        Agency = agency;
        Number = number;

        if (memberProfileId.HasValue)
            MemberProfileId = memberProfileId.Value;
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

    /// <summary>Transferência de titularidade da conta no mesmo lar (ex.: fusão de perfis vinculados).</summary>
    public void ReassignUserId(Guid newUserId)
    {
        if (newUserId == Guid.Empty)
            throw new DomainException("Usuário inválido.");

        UserId = newUserId;
    }

    /// <summary>Move a conta para outro lar (ex.: fusão ao aceitar convite).</summary>
    public void RelocateToHousehold(Guid newHouseholdId)
    {
        if (newHouseholdId == Guid.Empty)
            throw new DomainException("Lar inválido.");

        HouseholdId = newHouseholdId;
    }
}