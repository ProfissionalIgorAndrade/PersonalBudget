public interface ICategoryRepository
{
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task<Category?> GetByIdAsync(Guid id);
    Task<IEnumerable<Category>> GetByHouseholdAsync(Guid householdId);
    /// <summary>Todas as categorias do lar, inclusive inativas (migração / limpeza).</summary>
    Task<IEnumerable<Category>> GetAllByHouseholdAsync(Guid householdId);
    Task RemoveByHouseholdAsync(Guid householdId);
    Task SaveChangesAsync();
}
