using Microsoft.EntityFrameworkCore;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
        await SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
        => await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task<IReadOnlyList<RefreshToken>> GetNotRevokedByUserAsync(Guid userId)
        => await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
