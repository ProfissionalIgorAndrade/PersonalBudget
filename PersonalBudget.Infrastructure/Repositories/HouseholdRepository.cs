using Microsoft.EntityFrameworkCore;

public class HouseholdRepository : IHouseholdRepository
{
    private readonly AppDbContext _context;

    public HouseholdRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Household household)
    {
        _context.Households.Add(household);
        await SaveChangesAsync();
    }

    public async Task<Household?> GetByIdAsync(Guid id)
    {
        return await _context.Households.FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<IReadOnlyList<Household>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        if (ids.Count == 0)
            return Array.Empty<Household>();

        return await _context.Households
            .Where(h => ids.Contains(h.Id))
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
