public class CreditCard
{
    // Identidade
    public Guid Id { get; }

    // Contexto
    public Guid AccountId { get; }

    // Dados do cartão
    public string Name { get; }
    public string LastFourDigits { get; }
    public string Brand { get; }

    // Regras do cartão
    public Money CreditLimit { get; }
    public int ClosingDay { get; }
    public int DueDay { get; }

    // Estado
    public bool IsActive { get; private set; }

    public CreditCard(
        Guid accountId,
        string name,
        string lastFourDigits,
        string brand,
        Money creditLimit,
        int closingDay,
        int dueDay)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("Credit card must be linked to an account.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Credit card name is required.");

        if (string.IsNullOrWhiteSpace(lastFourDigits) || lastFourDigits.Length != 4)
            throw new DomainException("Credit card must have exactly 4 digits.");

        if (closingDay < 1 || closingDay > 28)
            throw new DomainException("Closing day must be between 1 and 28.");

        if (dueDay < 1 || dueDay > 28)
            throw new DomainException("Due day must be between 1 and 28.");

        Id = Guid.NewGuid();
        AccountId = accountId;
        Name = name;
        LastFourDigits = lastFourDigits;
        Brand = brand;
        CreditLimit = creditLimit;
        ClosingDay = closingDay;
        DueDay = dueDay;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
