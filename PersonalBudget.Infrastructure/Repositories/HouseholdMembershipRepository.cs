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
