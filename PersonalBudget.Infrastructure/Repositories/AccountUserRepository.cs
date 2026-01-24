using Microsoft.EntityFrameworkCore;

public class AccountUserRepository : IAccountUserRepository
{
    private readonly AppDbContext _context;

    public AccountUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AccountUser accountUser)
    {
        _context.AccountUsers.Add(accountUser);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid accountId, Guid userId)
    {
        return await _context.AccountUsers
            .AnyAsync(x => x.AccountId == accountId && x.UserId == userId);
    }
}
