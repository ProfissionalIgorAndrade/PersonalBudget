public interface IHouseholdRepository
{
    Task AddAsync(Household household);
    Task RemoveAsync(Household household);
    Task<Household?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Household>> GetByIdsAsync(IReadOnlyList<Guid> ids);
    Task SaveChangesAsync();
}
