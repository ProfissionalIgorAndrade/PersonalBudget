using PersonalBudget.Domain.Enums;

public class AccountUser
{
    public Guid Id { get; }

    public Guid AccountId { get; }
    public Guid UserId { get; }

    public AccountRole Role { get; }
    public DateTime JoinedAt { get; }

    public AccountUser(Guid accountId, Guid userId, AccountRole role)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("AccountId is required.");

        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        Id = Guid.NewGuid();
        AccountId = accountId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }
}
