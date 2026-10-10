using Microsoft.EntityFrameworkCore;

public class CategoryBudgetRepository : ICategoryBudgetRepository
{
    private readonly AppDbContext _context;

    public CategoryBudgetRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CategoryBudget budget)
    {
        _context.CategoryBudgets.Add(budget);
        await SaveChangesAsync();
    }

    public async Task<CategoryBudget?> GetByIdAsync(Guid id)
        => await _context.CategoryBudgets.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<CategoryBudget?> GetByCategoryAndMonthAsync(
        Guid householdId, Guid categoryId, int month, int year)
        => await _context.CategoryBudgets.FirstOrDefaultAsync(b =>
            b.HouseholdId == householdId &&
            b.CategoryId == categoryId &&
            b.Month == month &&
            b.Year == year);

    public async Task<IEnumerable<CategoryBudget>> GetByHouseholdAndMonthAsync(
        Guid householdId, int month, int year)
        => await _context.CategoryBudgets
            .Where(b => b.HouseholdId == householdId && b.Month == month && b.Year == year)
            .ToListAsync();

    public async Task UpdateAsync(CategoryBudget budget)
    {
        _context.CategoryBudgets.Update(budget);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(CategoryBudget budget)
    {
        _context.CategoryBudgets.Remove(budget);
        await SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
