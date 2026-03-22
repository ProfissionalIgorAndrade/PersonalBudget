using Microsoft.EntityFrameworkCore;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _context;

    public AccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Account account)
    {
        _context.Accounts.Add(account);
        await SaveChangesAsync();
    }

    public async Task<Account?> GetByIdAsync(Guid accountId)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task<IEnumerable<Account>> GetByHouseholdIdAsync(Guid householdId)
    {
        return await _context.Accounts
            .Where(a => a.HouseholdId == householdId && a.IsActive)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Account account)
    {
        _context.Accounts.Update(account);
        await SaveChangesAsync();
    }
}
