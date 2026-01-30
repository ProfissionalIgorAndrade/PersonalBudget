public class CreditCard
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public string Name { get; private set; }
    public decimal Limit { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public bool IsActive { get; private set; }

    private CreditCard(
        Guid userId,
        Guid accountId,
        string name,
        decimal limit,
        int closingDay,
        int dueDay)
    {
        if (limit <= 0)
            throw new DomainException("Credit card limit must be greater than zero.");

        Id = Guid.NewGuid();
        UserId = userId;
        AccountId = accountId;
        Name = name;
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
        IsActive = true;
    }

    protected CreditCard() { }

    public static CreditCard Create(
        Guid userId,
        Guid accountId,
        string name,
        decimal limit,
        int closingDay,
        int dueDay)
        => new(userId, accountId, name, limit, closingDay, dueDay);

    public void Update(string name, decimal limit, int closingDay, int dueDay)
    {
        if (!IsActive)
            throw new DomainException("Inactive credit card cannot be updated.");

        Name = name;
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
    }

    public void Deactivate()
        => IsActive = false;
}
