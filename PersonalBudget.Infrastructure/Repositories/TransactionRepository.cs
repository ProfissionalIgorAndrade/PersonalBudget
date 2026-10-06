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

        // Entities loaded via GetByRecurrenceIdAsync are already tracked.
        // UpdateRange only marks root scalar properties as Modified and skips
        // OwnsOne entries (Amount, Date, Description), causing those columns
        // to be omitted from the UPDATE. Let the snapshot change detector handle
        // all changes instead.
        foreach (var t in transactions)
        {
            if (_context.Entry(t).State == EntityState.Detached)
                _context.Transactions.Update(t);
        }

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

    public async Task<Dictionary<Guid, decimal>> GetBalancesByAccountIdsAsync(IEnumerable<Guid> accountIds)
    {
        var ids = accountIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, decimal>();

        return await _context.Transactions
            .Where(t => t.AccountId != null && ids.Contains(t.AccountId.Value))
            .GroupBy(t => t.AccountId!.Value)
            .Select(g => new
            {
                AccountId = g.Key,
                Balance = g.Sum(t => t.Type == TransactionType.Income
                    ? (decimal)t.Amount.Amount
                    : -(decimal)t.Amount.Amount)
            })
            .ToDictionaryAsync(x => x.AccountId, x => x.Balance);
    }

    public async Task<Dictionary<Guid, decimal>> GetBalancesByAccountIdsUntilAsync(
        IEnumerable<Guid> accountIds, DateTime cutoffDate)
    {
        var ids = accountIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, decimal>();

        // transaction_date é timestamptz e as datas são gravadas à meia-noite UTC.
        var cutoff = DateTime.SpecifyKind(cutoffDate.Date, DateTimeKind.Utc);

        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId != null && ids.Contains(t.AccountId.Value) && t.Date.Value <= cutoff)
            .GroupBy(t => t.AccountId!.Value)
            .Select(g => new
            {
                AccountId = g.Key,
                Balance = g.Sum(t => t.Type == TransactionType.Income
                    ? (decimal)t.Amount.Amount
                    : -(decimal)t.Amount.Amount)
            })
            .ToDictionaryAsync(x => x.AccountId, x => x.Balance);
    }
}
