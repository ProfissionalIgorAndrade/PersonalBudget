using Microsoft.EntityFrameworkCore;

public class SavingsBoxEventRepository : ISavingsBoxEventRepository
{
    private readonly AppDbContext _context;

    public SavingsBoxEventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SavingsBoxEvent savingsBoxEvent)
    {
        _context.SavingsBoxEvents.Add(savingsBoxEvent);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<SavingsBoxEvent>> ListByHouseholdAsync(Guid householdId)
    {
        return await _context.SavingsBoxEvents
            .AsNoTracking()
            .Where(e => e.HouseholdId == householdId)
            .OrderByDescending(e => e.OccurredAt)
            .ThenBy(e => e.Id)
            .ToListAsync();
    }
}
