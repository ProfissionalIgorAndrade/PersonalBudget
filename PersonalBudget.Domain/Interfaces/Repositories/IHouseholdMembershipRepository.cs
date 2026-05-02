public interface IHouseholdMembershipRepository
{
    Task AddAsync(HouseholdMembership membership);
    Task RemoveAsync(Guid userId, Guid householdId);
    /// <summary>Remove o usuário de todos os lares exceto <paramref name="keepHouseholdId"/>.</summary>
    Task RemoveAllExceptAsync(Guid userId, Guid keepHouseholdId);
    Task<bool> IsMemberAsync(Guid userId, Guid householdId);
    Task<IReadOnlyList<Guid>> GetHouseholdIdsByUserAsync(Guid userId);
    Task<IReadOnlyList<HouseholdMembership>> GetMembersByHouseholdAsync(Guid householdId);
    Task<HouseholdMembership?> GetAsync(Guid userId, Guid householdId);
    Task SaveChangesAsync();
}
