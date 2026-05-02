using Microsoft.EntityFrameworkCore;

public class HouseholdMembershipRepository : IHouseholdMembershipRepository
{
    private readonly AppDbContext _context;

    public HouseholdMembershipRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(HouseholdMembership membership)
    {
        _context.HouseholdMemberships.Add(membership);
        await SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid userId, Guid householdId)
    {
        var m = await _context.HouseholdMemberships
            .FirstOrDefaultAsync(x => x.UserId == userId && x.HouseholdId == householdId);
        if (m is null)
            return;

        _context.HouseholdMemberships.Remove(m);
        await SaveChangesAsync();
    }

    public async Task RemoveAllExceptAsync(Guid userId, Guid keepHouseholdId)
    {
        var toRemove = await _context.HouseholdMemberships
            .Where(x => x.UserId == userId && x.HouseholdId != keepHouseholdId)
            .ToListAsync();
        if (toRemove.Count == 0)
            return;

        _context.HouseholdMemberships.RemoveRange(toRemove);
        await SaveChangesAsync();
    }

    public async Task<bool> IsMemberAsync(Guid userId, Guid householdId)
    {
        return await _context.HouseholdMemberships
            .AnyAsync(m => m.UserId == userId && m.HouseholdId == householdId);
    }

    public async Task<IReadOnlyList<Guid>> GetHouseholdIdsByUserAsync(Guid userId)
    {
        return await _context.HouseholdMemberships
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.JoinedAt)
            .Select(m => m.HouseholdId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<HouseholdMembership>> GetMembersByHouseholdAsync(Guid householdId)
    {
        return await _context.HouseholdMemberships
            .AsNoTracking()
            .Where(m => m.HouseholdId == householdId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();
    }

    public async Task<HouseholdMembership?> GetAsync(Guid userId, Guid householdId)
    {
        return await _context.HouseholdMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.HouseholdId == householdId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
