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

    public async Task<IEnumerable<CreditCard>> GetByUserAsync(Guid userId)
    {
        return await _context.CreditCards
            .Where(c => c.UserId == userId && c.IsActive)
            .ToListAsync();
    }
}
