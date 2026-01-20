public class CreditCard
{
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

    public Guid Id { get; }
    public Guid AccountId { get; }
    public string Name { get; }
    public string LastFourDigits { get; }
    public string Brand { get; }
    public Money CreditLimit { get; }
    public int ClosingDay { get; }
    public int DueDay { get; }
    public bool IsActive { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
    }
}
