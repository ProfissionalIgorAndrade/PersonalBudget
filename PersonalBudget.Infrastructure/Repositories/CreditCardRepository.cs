using Microsoft.EntityFrameworkCore;

public class CreditCardRepository : ICreditCardRepository
{
    private readonly AppDbContext _context;

    public CreditCardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CreditCard creditCard)
    {
        _context.CreditCards.Add(creditCard);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(CreditCard creditCard)
    {
        _context.CreditCards.Update(creditCard);
        await SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<CreditCard?> GetByIdAsync(Guid id)
    {
        return await _context.CreditCards
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CreditCard?> GetByIdWithStatementsAsync(Guid id)
    {
        return await _context.CreditCards
            .Include(c => c.Statements)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<CreditCard>> GetByHouseholdAsync(Guid householdId)
    {
        return await _context.CreditCards
            .Where(c => c.HouseholdId == householdId && c.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<CreditCard>> GetAllByHouseholdAsync(Guid householdId)
    {
        return await _context.CreditCards
            .Where(c => c.HouseholdId == householdId)
            .ToListAsync();
    }

    public async Task<IEnumerable<CreditCard>> GetAllByHouseholdAndUserAsync(Guid householdId, Guid userId)
    {
        return await _context.CreditCards
            .Where(c => c.HouseholdId == householdId && c.UserId == userId)
            .ToListAsync();
    }

    public async Task BulkUpdateAsync(IReadOnlyList<CreditCard> creditCards)
    {
        if (creditCards.Count == 0)
            return;

        _context.CreditCards.UpdateRange(creditCards);
        await SaveChangesAsync();
    }

    public async Task DeactivateByAccountIdAsync(Guid accountId)
    {
        var creditCards = await _context.CreditCards
            .Where(c => c.AccountId == accountId && c.IsActive)
            .ToListAsync();

        foreach (var card in creditCards)
            card.Deactivate();

        if (creditCards.Count > 0)
            await _context.SaveChangesAsync();
    }
}
