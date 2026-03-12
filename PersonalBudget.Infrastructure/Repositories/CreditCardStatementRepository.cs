
using Microsoft.EntityFrameworkCore;

namespace PersonalBudget.Infrastructure.Repositories;

public class CreditCardStatementRepository : ICreditCardStatementRepository
{
    private readonly AppDbContext _context;

    public CreditCardStatementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CreditCardStatement statement)
    {
        if (_context.Entry(statement).State != EntityState.Detached)
            return;

        await _context.CreditCardStatements.AddAsync(statement);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(CreditCardStatement statement)
    {
        _context.CreditCardStatements.Update(statement);
        await SaveChangesAsync();
    }

    public async Task<CreditCardStatement?> GetOpenStatementForDateAsync(Guid creditCardId, DateTime date)
    {
        return await _context.CreditCardStatements
            .FirstOrDefaultAsync(x =>
                x.CreditCardId == creditCardId &&
                x.PeriodStart <= date &&
                x.PeriodEnd >= date &&
                x.Status == BillStatus.Open);
    }

    public async Task<CreditCardStatement?> GetByIdAsync(Guid id)
    {
        return await _context.CreditCardStatements
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CreditCardStatement?> GetByCreditCardAndClosingMonthYearAsync(Guid creditCardId, int month, int year)
    {
        return await _context.CreditCardStatements
            .FirstOrDefaultAsync(x =>
                x.CreditCardId == creditCardId &&
                x.ClosingMonth == month &&
                x.ClosingYear == year);
    }

    public async Task<List<CreditCardStatement>> GetByCreditCardAsync(Guid creditCardId)
    {
        return await _context.CreditCardStatements
            .Where(x => x.CreditCardId == creditCardId)
            .OrderByDescending(x => x.PeriodStart)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

}
