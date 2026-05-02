public interface IHouseholdInviteRepository
{
    Task AddAsync(HouseholdInvite invite);
    Task<HouseholdInvite?> GetByTokenAsync(string token);
    Task<IReadOnlyList<HouseholdInvite>> GetPendingByHouseholdAndEmailAsync(Guid householdId, string emailNormalized);
    Task<IReadOnlyList<HouseholdInvite>> GetPendingInvitesByHouseholdAsync(Guid householdId);
    Task<IReadOnlyList<HouseholdInvite>> GetPendingInvitesByInviteeEmailAsync(string emailNormalized);
    Task SaveChangesAsync();
}
