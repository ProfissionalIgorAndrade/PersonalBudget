
using Microsoft.EntityFrameworkCore;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Category category)
    {
        _context.Categories.Add(category);
        await SaveChangesAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Category>> GetByHouseholdAsync(Guid householdId)
    {
        return await _context.Categories
            .Where(c => c.HouseholdId == householdId && c.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetAllByHouseholdAsync(Guid householdId)
    {
        return await _context.Categories
            .Where(c => c.HouseholdId == householdId)
            .ToListAsync();
    }

    public async Task RemoveByHouseholdAsync(Guid householdId)
    {
        var categories = await _context.Categories
            .Where(c => c.HouseholdId == householdId)
            .ToListAsync();
        if (categories.Count == 0)
            return;

        _context.Categories.RemoveRange(categories);
        await SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await SaveChangesAsync();
    }
}