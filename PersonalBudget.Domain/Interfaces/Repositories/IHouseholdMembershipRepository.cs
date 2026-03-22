public interface IHouseholdMembershipRepository
{
    Task AddAsync(HouseholdMembership membership);
    Task<bool> IsMemberAsync(Guid userId, Guid householdId);
    Task<IReadOnlyList<Guid>> GetHouseholdIdsByUserAsync(Guid userId);
    Task<HouseholdMembership?> GetAsync(Guid userId, Guid householdId);
    Task SaveChangesAsync();
}
