using Microsoft.EntityFrameworkCore;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await SaveChangesAsync();
    }

    public async Task BulkUpdateAsync(IReadOnlyList<Transaction> transactions)
    {
        if (transactions.Count == 0)
            return;

        _context.Transactions.UpdateRange(transactions);
        await SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Transaction?> GetByIdAsync(Guid transactionId)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    public async Task<IEnumerable<Transaction>> GetByIdsAsync(IEnumerable<Guid> transactionIds)
    {
        var idList = transactionIds.ToList();
        if (idList.Count == 0)
            return Array.Empty<Transaction>();

        return await _context.Transactions
            .Where(t => idList.Contains(t.Id))
            .ToListAsync();
    }

    public async Task DeleteManyAsync(IEnumerable<Transaction> transactions)
    {
        _context.Transactions.RemoveRange(transactions);
        await SaveChangesAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByAccountAsync(Guid accountId)
    {
        return await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Date.Value)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByHouseholdAsync(Guid householdId)
    {
        return await _context.Transactions
            .Where(t => t.HouseholdId == householdId)
            .OrderByDescending(t => t.Date.Value)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Transaction>> GetByHouseholdAndAttributionProfileAsync(
        Guid householdId,
        Guid attributionProfileId)
    {
        return await _context.Transactions
            .Where(t =>
                t.HouseholdId == householdId &&
                t.AttributionProfileId == attributionProfileId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByStatementIdAsync(Guid statementId)
    {
        return await _context.Transactions
            .Where(t => t.StatementId == statementId)
            .OrderBy(t => t.Date.Value)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Transaction>> GetByRecurrenceIdAsync(Guid recurrenceId, Guid householdId)
    {
        return await _context.Transactions
            .Where(t => t.RecurrenceId == recurrenceId && t.HouseholdId == householdId)
            .OrderBy(t => t.Date.Value)
            .ToListAsync();
    }
}
