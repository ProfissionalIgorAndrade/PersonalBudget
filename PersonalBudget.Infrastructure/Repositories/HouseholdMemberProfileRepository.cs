using Microsoft.EntityFrameworkCore;

public class HouseholdMemberProfileRepository : IHouseholdMemberProfileRepository
{
    private readonly AppDbContext _context;

    public HouseholdMemberProfileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(HouseholdMemberProfile profile)
    {
        _context.HouseholdMemberProfiles.Add(profile);
        await SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<HouseholdMemberProfile> profiles)
    {
        _context.HouseholdMemberProfiles.AddRange(profiles);
        await SaveChangesAsync();
    }

    public async Task RemoveAsync(HouseholdMemberProfile profile)
    {
        _context.HouseholdMemberProfiles.Remove(profile);
        await SaveChangesAsync();
    }

    public async Task BulkUpdateAsync(IReadOnlyList<HouseholdMemberProfile> profiles)
    {
        if (profiles.Count == 0)
            return;

        _context.HouseholdMemberProfiles.UpdateRange(profiles);
        await SaveChangesAsync();
    }

    public async Task<IReadOnlyList<HouseholdMemberProfile>> GetByHouseholdAsync(Guid householdId)
    {
        return await _context.HouseholdMemberProfiles
            .Where(p => p.HouseholdId == householdId)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .ToListAsync();
    }

    public async Task<HouseholdMemberProfile?> GetByIdAsync(Guid id)
    {
        return await _context.HouseholdMemberProfiles.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<HouseholdMemberProfile?> GetLinkedProfileForUserAsync(Guid householdId, Guid userId)
    {
        return await _context.HouseholdMemberProfiles
            .FirstOrDefaultAsync(p =>
                p.HouseholdId == householdId &&
                p.Kind == HouseholdMemberProfileKind.LinkedUser &&
                p.UserId == userId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
