public interface IHouseholdMemberProfileRepository
{
    Task AddAsync(HouseholdMemberProfile profile);
    Task AddRangeAsync(IEnumerable<HouseholdMemberProfile> profiles);
    Task RemoveAsync(HouseholdMemberProfile profile);
    Task BulkUpdateAsync(IReadOnlyList<HouseholdMemberProfile> profiles);
    Task<IReadOnlyList<HouseholdMemberProfile>> GetByHouseholdAsync(Guid householdId);
    Task<HouseholdMemberProfile?> GetByIdAsync(Guid id);
    Task<HouseholdMemberProfile?> GetLinkedProfileForUserAsync(Guid householdId, Guid userId);
    Task SaveChangesAsync();
}
