using Microsoft.EntityFrameworkCore;

public class HouseholdInviteRepository : IHouseholdInviteRepository
{
    private readonly AppDbContext _context;

    public HouseholdInviteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(HouseholdInvite invite)
    {
        _context.HouseholdInvites.Add(invite);
        await SaveChangesAsync();
    }

    public async Task<HouseholdInvite?> GetByTokenAsync(string token)
    {
        return await _context.HouseholdInvites
            .FirstOrDefaultAsync(i => i.Token == token);
    }

    public async Task<IReadOnlyList<HouseholdInvite>> GetPendingByHouseholdAndEmailAsync(
        Guid householdId,
        string emailNormalized)
    {
        var n = emailNormalized.Trim().ToLowerInvariant();
        return await _context.HouseholdInvites
            .Where(i =>
                i.HouseholdId == householdId &&
                i.InviteeEmailNormalized == n &&
                i.Status == HouseholdInviteStatus.Pending)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
