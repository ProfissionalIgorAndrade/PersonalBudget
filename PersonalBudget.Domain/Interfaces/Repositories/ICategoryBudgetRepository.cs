
public interface ICategoryBudgetRepository
{
    Task AddAsync(CategoryBudget budget);
    Task<CategoryBudget?> GetByIdAsync(Guid id);
    Task<CategoryBudget?> GetByCategoryAndMonthAsync(Guid householdId, Guid categoryId, int month, int year);
    Task<IEnumerable<CategoryBudget>> GetByHouseholdAndMonthAsync(Guid householdId, int month, int year);
    Task UpdateAsync(CategoryBudget budget);
    Task DeleteAsync(CategoryBudget budget);
    Task SaveChangesAsync();
}
