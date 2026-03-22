using PersonalBudget.Application.Interfaces;

public class ActiveHouseholdResolver : IActiveHouseholdResolver
{
    private readonly IHouseholdMembershipRepository _memberships;

    public ActiveHouseholdResolver(IHouseholdMembershipRepository memberships)
    {
        _memberships = memberships;
    }

    public async Task<Guid> ResolveAsync(Guid userId, Guid? householdIdFromHeader)
    {
        if (householdIdFromHeader is { } hid && await _memberships.IsMemberAsync(userId, hid))
            return hid;

        var ids = await _memberships.GetHouseholdIdsByUserAsync(userId);
        if (ids.Count == 0)
            throw new DomainException("Usuário sem lar configurado.");

        return ids[0];
    }
}
