using Microsoft.EntityFrameworkCore;
using PersonalBudget.Application.DTOs.CreditCard;
using PersonalBudget.Application.DTOs.Household;

public class TransactionQueryRepository : ITransactionQueryRepository
{
    private readonly AppDbContext _context;

    public TransactionQueryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetAllTransactionByCreditCardStatementAndMonthYearQuery(Guid householdId, Guid creditCardId, int month, int year)
    {
        return await
            (from t in _context.Transactions
             join s in _context.CreditCardStatements
                 on t.CreditCardId equals s.CreditCardId
             join a in _context.Accounts
                 on t.AccountId equals a.Id
             join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
             from c in _context.Categories
                 .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                 .DefaultIfEmpty()
             from cc in _context.CreditCards
                 .Where(cc => cc.Id == t.CreditCardId)
                 .DefaultIfEmpty()
             where t.HouseholdId == householdId
                 && t.CreditCardId == creditCardId
                 && t.Date.Value.Month == month
                 && t.Date.Value.Year == year
                 && s.Status == BillStatus.Open
                 && t.Date.Value >= s.PeriodStart
                 && t.Date.Value <= s.PeriodEnd
             orderby t.Date.Value descending
             select new GetAllTransactionByUserResponse(
                 t.Id,
                 t.AccountId,
                 a.Agency.Value,
                 t.CategoryId,
                 c != null ? c.Name : null,
                 c != null ? c.Type.ToString() : null,
                 t.CreditCardId,
                 cc != null ? cc.Name : null,
                 t.TransferId,
                 t.Type.ToString(),
                 t.Status.ToString(),
                 t.PaymentMethod.ToString(),
                 t.Frequency.ToString(),
                 t.ExpirationDate,
                 t.DueDate,
                 t.Amount.Amount,
                 t.Date.Value,
                 t.Description.Value,
                 p.Id,
                 p.DisplayName
             ))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StatementTransactionItemDto>> GetTransactionDetailsByStatementIdAsync(Guid statementId)
    {
        var query =
            from t in _context.Transactions
            join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
            join c in _context.Categories on t.CategoryId equals c.Id into categoryJoin
            from c in categoryJoin.DefaultIfEmpty()
            where t.StatementId == statementId
            orderby t.Date.Value descending
            select new StatementTransactionItemDto(
                t.Id,
                t.Date.Value,
                t.DueDate,
                t.Description.Value,
                t.Amount.Amount,
                t.CategoryId,
                c != null ? c.Name : null,
                t.Type.ToString(),
                t.Status.ToString(),
                t.Frequency.ToString(),
                p.Id,
                p.DisplayName
            );

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task<(IReadOnlyList<StatementTransactionItemDto> Items, int TotalCount)> GetTransactionDetailsByStatementIdPagedAsync(
        Guid statementId, int page, int pageSize)
    {
        var query =
            from t in _context.Transactions
            join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
            join c in _context.Categories on t.CategoryId equals c.Id into categoryJoin
            from c in categoryJoin.DefaultIfEmpty()
            where t.StatementId == statementId
            orderby t.Date.Value descending
            select new StatementTransactionItemDto(
                t.Id,
                t.Date.Value,
                t.DueDate,
                t.Description.Value,
                t.Amount.Amount,
                t.CategoryId,
                c != null ? c.Name : null,
                t.Type.ToString(),
                t.Status.ToString(),
                t.Frequency.ToString(),
                p.Id,
                p.DisplayName
            );

        var totalCount = await query.AsNoTracking().CountAsync();
        var items = await query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<decimal> GetStatementNetTotalAsync(Guid statementId)
    {
        var net = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.StatementId == statementId)
            .Select(t => t.Type == TransactionType.Expense ? t.Amount.Amount : -t.Amount.Amount)
            .SumAsync();
        return net < 0 ? 0 : net;
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByHouseholdAndMonthAsync(Guid householdId, int month, int year)
    {
        var query =
            from t in _context.Transactions
            join a in _context.Accounts
                on t.AccountId equals a.Id
            join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            from cc in _context.CreditCards
                .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                .DefaultIfEmpty()
            where t.HouseholdId == householdId && t.Date.Value.Month == month && t.Date.Value.Year == year
            orderby t.Date.Value descending
            select new GetAllTransactionByUserResponse(
                t.Id,
                t.AccountId,
                a.Agency.Value,
                t.CategoryId,
                c != null ? c.Name : null,
                c != null ? c.Type.ToString() : null,
                t.CreditCardId,
                cc != null ? cc.Name : null,
                t.TransferId,
                t.Type.ToString(),
                t.Status.ToString(),
                t.PaymentMethod.ToString(),
                t.Frequency.ToString(),
                t.ExpirationDate,
                t.DueDate,
                t.Amount.Amount,
                t.Date.Value,
                t.Description.Value,
                p.Id,
                p.DisplayName
            );

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByHouseholdAndMonthPagedAsync(
        Guid householdId, int month, int year, int page, int pageSize)
    {
        var query =
            from t in _context.Transactions
            join a in _context.Accounts
                on t.AccountId equals a.Id
            join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            from cc in _context.CreditCards
                .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                .DefaultIfEmpty()
            where t.HouseholdId == householdId && t.Date.Value.Month == month && t.Date.Value.Year == year
            orderby t.Date.Value descending
            select new GetAllTransactionByUserResponse(
                t.Id,
                t.AccountId,
                a.Agency.Value,
                t.CategoryId,
                c != null ? c.Name : null,
                c != null ? c.Type.ToString() : null,
                t.CreditCardId,
                cc != null ? cc.Name : null,
                t.TransferId,
                t.Type.ToString(),
                t.Status.ToString(),
                t.PaymentMethod.ToString(),
                t.Frequency.ToString(),
                t.ExpirationDate,
                t.DueDate,
                t.Amount.Amount,
                t.Date.Value,
                t.Description.Value,
                p.Id,
                p.DisplayName
            );

        var totalCount = await query.AsNoTracking().CountAsync();
        var items = await query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(Guid householdId, Guid accountId, int month, int year)
    {
        var query =
            from t in _context.Transactions
            join a in _context.Accounts
                on t.AccountId equals a.Id
            join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            from cc in _context.CreditCards
                .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                .DefaultIfEmpty()
            where t.HouseholdId == householdId
               && t.AccountId == accountId
               && t.Date.Value.Month == month
               && t.Date.Value.Year == year
               && t.PaymentMethod != PaymentMethod.CreditCard
            orderby t.Date.Value descending
            select new GetAllTransactionByUserResponse(
                t.Id,
                t.AccountId,
                a.Agency.Value,
                t.CategoryId,
                c != null ? c.Name : null,
                c != null ? c.Type.ToString() : null,
                t.CreditCardId,
                cc != null ? cc.Name : null,
                t.TransferId,
                t.Type.ToString(),
                t.Status.ToString(),
                t.PaymentMethod.ToString(),
                t.Frequency.ToString(),
                t.ExpirationDate,
                t.DueDate,
                t.Amount.Amount,
                t.Date.Value,
                t.Description.Value,
                p.Id,
                p.DisplayName
            );

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByAccountAndMonthPagedAsync(
        Guid householdId, Guid accountId, int month, int year, int page, int pageSize)
    {
        var query =
            from t in _context.Transactions
            join a in _context.Accounts
                on t.AccountId equals a.Id
            join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            from cc in _context.CreditCards
                .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                .DefaultIfEmpty()
            where t.HouseholdId == householdId
               && t.AccountId == accountId
               && t.Date.Value.Month == month
               && t.Date.Value.Year == year
               && t.PaymentMethod != PaymentMethod.CreditCard
            orderby t.Date.Value descending
            select new GetAllTransactionByUserResponse(
                t.Id,
                t.AccountId,
                a.Agency.Value,
                t.CategoryId,
                c != null ? c.Name : null,
                c != null ? c.Type.ToString() : null,
                t.CreditCardId,
                cc != null ? cc.Name : null,
                t.TransferId,
                t.Type.ToString(),
                t.Status.ToString(),
                t.PaymentMethod.ToString(),
                t.Frequency.ToString(),
                t.ExpirationDate,
                t.DueDate,
                t.Amount.Amount,
                t.Date.Value,
                t.Description.Value,
                p.Id,
                p.DisplayName
            );

        var totalCount = await query.AsNoTracking().CountAsync();
        var items = await query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByHouseholdAsync(Guid householdId)
    {
        return await
            (from t in _context.Transactions
             join a in _context.Accounts
                 on t.AccountId equals a.Id
             join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
             from c in _context.Categories
                 .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                 .DefaultIfEmpty()
             from cc in _context.CreditCards
                 .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                 .DefaultIfEmpty()
             where t.HouseholdId == householdId
             orderby t.Date.Value descending
             select new GetAllTransactionByUserResponse(
                 t.Id,
                    t.AccountId,
                    a.Agency.Value,
                    t.CategoryId,
                    c != null ? c.Name : null,
                    c != null ? c.Type.ToString() : null,
                    t.CreditCardId,
                    cc != null ? cc.Name : null,
                    t.TransferId,
                    t.Type.ToString(),
                    t.Status.ToString(),
                    t.PaymentMethod.ToString(),
                    t.Frequency.ToString(),
                    t.ExpirationDate,
                    t.DueDate,
                    t.Amount.Amount,
                    t.Date.Value,
                    t.Description.Value,
                    p.Id,
                    p.DisplayName
             ))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<HouseholdProfileSummaryRow>> GetHouseholdSummaryByProfileAsync(
        Guid householdId, int month, int year)
    {
        var profiles = await _context.HouseholdMemberProfiles.AsNoTracking()
            .Where(p => p.HouseholdId == householdId)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .Select(p => new { p.Id, p.DisplayName })
            .ToListAsync();

        var sumsByProfile = await _context.Transactions.AsNoTracking()
            .Where(t => t.HouseholdId == householdId
                        && t.Date.Value.Month == month
                        && t.Date.Value.Year == year)
            .GroupBy(t => t.AttributionProfileId)
            .Select(g => new
            {
                ProfileId = g.Key,
                TotalExpenses = g.Sum(x => x.Type == TransactionType.Expense ? x.Amount.Amount : 0m),
                TotalIncome = g.Sum(x => x.Type == TransactionType.Income ? x.Amount.Amount : 0m)
            })
            .ToDictionaryAsync(x => x.ProfileId, x => (x.TotalExpenses, x.TotalIncome));

        return profiles.Select(p =>
        {
            var has = sumsByProfile.TryGetValue(p.Id, out var totals);
            return new HouseholdProfileSummaryRow(
                p.Id,
                p.DisplayName,
                has ? totals.TotalExpenses : 0m,
                has ? totals.TotalIncome : 0m);
        }).ToList();
    }
}