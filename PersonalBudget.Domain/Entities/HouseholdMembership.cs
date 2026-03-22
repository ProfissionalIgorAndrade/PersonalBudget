public class HouseholdMembership
{
    public Guid Id { get; private set; }
    public Guid HouseholdId { get; private set; }
    public Guid UserId { get; private set; }
    public HouseholdRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    protected HouseholdMembership() { }

    public HouseholdMembership(Guid householdId, Guid userId, HouseholdRole role)
    {
        if (householdId == Guid.Empty || userId == Guid.Empty)
            throw new DomainException("Lar e usuário são obrigatórios.");

        Id = Guid.NewGuid();
        HouseholdId = householdId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }

    public static HouseholdMembership CreateOwner(Guid householdId, Guid userId)
        => new(householdId, userId, HouseholdRole.Owner);

    public static HouseholdMembership CreateMember(Guid householdId, Guid userId)
        => new(householdId, userId, HouseholdRole.Member);
}
