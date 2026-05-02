using PersonalBudget.Application.Interfaces;

public class HouseholdProvisioningService : IHouseholdProvisioningService
{
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdMembershipRepository _memberships;
    private readonly IHouseholdMemberProfileRepository _profiles;

    public HouseholdProvisioningService(
        IHouseholdRepository households,
        IHouseholdMembershipRepository memberships,
        IHouseholdMemberProfileRepository profiles)
    {
        _households = households;
        _memberships = memberships;
        _profiles = profiles;
    }

    public async Task ProvisionNewUserAsync(Guid userId, string displayName)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? "Eu" : displayName.Trim();
        var household = Household.Create($"{name} — lar");
        await _households.AddAsync(household);
        await _memberships.AddAsync(HouseholdMembership.CreateOwner(household.Id, userId));
        await _profiles.AddAsync(HouseholdMemberProfile.CreateLinkedUser(household.Id, userId, name, 0));
    }
}
