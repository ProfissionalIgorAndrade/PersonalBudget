using Microsoft.EntityFrameworkCore;
using PersonalBudget.Application.DTOs.CreditCard;
using PersonalBudget.Application.DTOs.Dashboard;
using PersonalBudget.Application.DTOs.Household;
using PersonalBudget.Application.DTOs.Transaction;

public class TransactionQueryRepository : ITransactionQueryRepository
{
    private readonly AppDbContext _context;

    public TransactionQueryRepository(AppDbContext context)
    {
        _context = context;
    }

    // ─── shared projection helper ──────────────────────────────────────────────

    private IQueryable<GetAllTransactionByUserResponse> BuildFullTransactionQuery(Guid householdId)
    {
        return
            from t in _context.Transactions
            join a in _context.Accounts on t.AccountId equals a.Id
            join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            from cc in _context.CreditCards
                .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                .DefaultIfEmpty()
            from s in _context.CreditCardStatements
                .Where(s => t.StatementId != null && s.Id == t.StatementId)
                .DefaultIfEmpty()
            where t.HouseholdId == householdId
            orderby t.Date.Value descending
            select new GetAllTransactionByUserResponse(
                t.Id, t.AccountId, a.Agency.Value,
                t.CategoryId, c != null ? c.Name : null, c != null ? c.Type.ToString() : null,
                t.CreditCardId, cc != null ? cc.Name : null,
                t.TransferId, t.Type.ToString(), t.Status.ToString(),
                t.PaymentMethod.ToString(), t.Frequency.ToString(),
                t.ExpirationDate, t.DueDate,
                t.Amount.Amount, t.Date.Value, t.Description.Value,
                p.Id, p.DisplayName, t.RecurrenceId,
                s != null ? s.ClosingMonth : (int?)null,
                s != null ? s.ClosingYear : (int?)null);
    }

    // ─── existing methods ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetAllTransactionByCreditCardStatementAndMonthYearQuery(Guid householdId, Guid creditCardId, int month, int year)
    {
        return await
            (from t in _context.Transactions
             join s in _context.CreditCardStatements
                 on t.StatementId equals s.Id
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
                 && s.ClosingMonth == month
                 && s.ClosingYear == year
             orderby t.Date.Value descending
             select new GetAllTransactionByUserResponse(
                 t.Id, t.AccountId, a.Agency.Value,
                 t.CategoryId, c != null ? c.Name : null, c != null ? c.Type.ToString() : null,
                 t.CreditCardId, cc != null ? cc.Name : null,
                 t.TransferId, t.Type.ToString(), t.Status.ToString(),
                 t.PaymentMethod.ToString(), t.Frequency.ToString(),
                 t.ExpirationDate, t.DueDate,
                 t.Amount.Amount, t.Date.Value, t.Description.Value,
                 p.Id, p.DisplayName, t.RecurrenceId,
                 s.ClosingMonth, s.ClosingYear))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StatementTransactionItemDto>> GetTransactionDetailsByStatementAsync(Guid statementId)
    {
        var query =
            from t in _context.Transactions
            join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
            join c in _context.Categories on t.CategoryId equals c.Id into categoryJoin
            from c in categoryJoin.DefaultIfEmpty()
            where t.StatementId == statementId
            orderby t.Date.Value descending
            select new StatementTransactionItemDto(
                t.Id, t.Date.Value, t.DueDate, t.Description.Value, t.Amount.Amount,
                t.CategoryId, c != null ? c.Name : null,
                t.Type.ToString(), t.Status.ToString(), t.Frequency.ToString(),
                p.Id, p.DisplayName);

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task<(IReadOnlyList<StatementTransactionItemDto> Items, int TotalCount)> GetTransactionDetailsByStatementPagedAsync(
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
                t.Id, t.Date.Value, t.DueDate, t.Description.Value, t.Amount.Amount,
                t.CategoryId, c != null ? c.Name : null,
                t.Type.ToString(), t.Status.ToString(), t.Frequency.ToString(),
                p.Id, p.DisplayName);

        var totalCount = await query.AsNoTracking().CountAsync();
        var items = await query.AsNoTracking().Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
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

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByHouseholdAsync(Guid householdId)
    {
        return await BuildFullTransactionQuery(householdId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByHouseholdAndMonthAsync(
        Guid householdId, int month, int year, TransactionFrequency? frequency = null)
    {
        var query = BuildFullTransactionQuery(householdId)
            .Where(t => (t.CreditCardId == null && t.Date.Month == month && t.Date.Year == year)
                     || (t.CreditCardId != null && t.StatementMonth == month && t.StatementYear == year));

        if (frequency.HasValue)
            query = query.Where(t => t.Frequency == frequency.Value.ToString());

        return await query
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount, decimal PeriodTotalIncome, decimal PeriodTotalExpense)> GetByHouseholdAndMonthPagedAsync(
        Guid householdId, int month, int year, int page, int pageSize, TransactionFrequency? frequency = null)
    {
        var query = BuildFullTransactionQuery(householdId)
            .Where(t => (t.CreditCardId == null && t.Date.Month == month && t.Date.Year == year)
                     || (t.CreditCardId != null && t.StatementMonth == month && t.StatementYear == year));

        if (frequency.HasValue)
            query = query.Where(t => t.Frequency == frequency.Value.ToString());

        var allInPeriod = await query.AsNoTracking().ToListAsync();

        var totalCount = allInPeriod.Count;
        var periodTotalIncome = allInPeriod.Where(t => t.Type == TransactionType.Income.ToString()).Sum(t => t.Amount);
        var periodTotalExpense = allInPeriod.Where(t => t.Type == TransactionType.Expense.ToString()).Sum(t => t.Amount);
        var items = allInPeriod
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, totalCount, periodTotalIncome, periodTotalExpense);
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(
        Guid householdId, Guid accountId, int month, int year, TransactionFrequency? frequency = null)
    {
        return await BuildAccountAndMonthQuery(householdId, accountId, month, year, frequency)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByAccountAndMonthPagedAsync(
        Guid householdId, Guid accountId, int month, int year, int page, int pageSize, TransactionFrequency? frequency = null)
    {
        var query = BuildAccountAndMonthQuery(householdId, accountId, month, year, frequency)
            .AsNoTracking();

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    private IQueryable<GetAllTransactionByUserResponse> BuildAccountAndMonthQuery(
        Guid householdId, Guid accountId, int month, int year, TransactionFrequency? frequency)
    {
        var transactions = _context.Transactions
            .Where(t => t.HouseholdId == householdId
                     && t.AccountId == accountId
                     && t.Date.Value.Month == month
                     && t.Date.Value.Year == year
                     && t.PaymentMethod != PaymentMethod.CreditCard);

        if (frequency.HasValue)
            transactions = transactions.Where(t => t.Frequency == frequency.Value);

        return from t in transactions
               join a in _context.Accounts on t.AccountId equals a.Id
               join p in _context.HouseholdMemberProfiles on t.AttributionProfileId equals p.Id
               from c in _context.Categories
                   .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                   .DefaultIfEmpty()
               from cc in _context.CreditCards
                   .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                   .DefaultIfEmpty()
               orderby t.Date.Value descending
               select new GetAllTransactionByUserResponse(
                   t.Id, t.AccountId, a.Agency.Value,
                   t.CategoryId, c != null ? c.Name : null, c != null ? c.Type.ToString() : null,
                   t.CreditCardId, cc != null ? cc.Name : null,
                   t.TransferId, t.Type.ToString(), t.Status.ToString(),
                   t.PaymentMethod.ToString(), t.Frequency.ToString(),
                   t.ExpirationDate, t.DueDate,
                   t.Amount.Amount, t.Date.Value, t.Description.Value,
                   p.Id, p.DisplayName, t.RecurrenceId,
                   null, null);
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

        var sumsByProfile = await (
            from t in _context.Transactions
            from s in _context.CreditCardStatements
                .Where(s => t.StatementId != null && s.Id == t.StatementId)
                .DefaultIfEmpty()
            where t.HouseholdId == householdId
               && ((t.CreditCardId == null && t.Date.Value.Month == month && t.Date.Value.Year == year)
                || (t.CreditCardId != null && s != null && s.ClosingMonth == month && s.ClosingYear == year))
            group t by t.AttributionProfileId into g
            select new
            {
                ProfileId = g.Key,
                TotalExpenses = g.Sum(x => x.Type == TransactionType.Expense ? x.Amount.Amount : 0m),
                TotalIncome   = g.Sum(x => x.Type == TransactionType.Income   ? x.Amount.Amount : 0m)
            }
        ).AsNoTracking().ToDictionaryAsync(x => x.ProfileId, x => (x.TotalExpenses, x.TotalIncome));

        return profiles.Select(p =>
        {
            var has = sumsByProfile.TryGetValue(p.Id, out var totals);
            var expenses = has ? totals.TotalExpenses : 0m;
            var income   = has ? totals.TotalIncome   : 0m;
            var color    = GenerateAvatarColor(p.Id);
            return new HouseholdProfileSummaryRow(p.Id, p.DisplayName, expenses, income, income - expenses, color);
        }).ToList();
    }

    // ─── new methods ────────────────────────────────────────────────────────────

    public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(Guid householdId, int month, int year)
    {
        var transactions = await (
            from t in _context.Transactions
            from s in _context.CreditCardStatements
                .Where(s => t.StatementId != null && s.Id == t.StatementId)
                .DefaultIfEmpty()
            where t.HouseholdId == householdId
               && ((t.CreditCardId == null && t.Date.Value.Month == month && t.Date.Value.Year == year)
                || (t.CreditCardId != null && s != null && s.ClosingMonth == month && s.ClosingYear == year))
            select new
            {
                t.Type,
                t.Frequency,
                t.PaymentMethod,
                Amount = t.Amount.Amount
            }
        ).AsNoTracking().ToListAsync();

        var totalIncome            = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense           = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var fixedExpenses          = transactions.Where(t => t.Type == TransactionType.Expense && t.Frequency == TransactionFrequency.Fixed).Sum(t => t.Amount);
        var variableExpenses       = transactions.Where(t => t.Type == TransactionType.Expense && t.Frequency == TransactionFrequency.Variable).Sum(t => t.Amount);
        var installmentsThisMonth  = transactions.Where(t => t.Frequency == TransactionFrequency.Installments).Sum(t => t.Amount);
        var creditCardInvoice      = transactions.Where(t => t.Type == TransactionType.Expense && t.PaymentMethod == PaymentMethod.CreditCard).Sum(t => t.Amount);
        var balance                = totalIncome - totalExpense;
        var committedIncomePercent = totalIncome > 0 ? Math.Round(fixedExpenses / totalIncome * 100, 2) : 0m;
        var savingsPercent         = totalIncome > 0 ? Math.Round(balance / totalIncome * 100, 2) : 0m;

        return new DashboardSummaryResponse(
            month, year,
            totalIncome, totalExpense, balance,
            fixedExpenses, variableExpenses, installmentsThisMonth, creditCardInvoice,
            committedIncomePercent, savingsPercent,
            transactions.Count);
    }

    public async Task<IReadOnlyList<MonthlyTrendResponse>> GetTransactionTrendsAsync(
        Guid householdId, int months, int endMonth, int endYear)
    {
        var endDate   = new DateTime(endYear, endMonth, 1);
        var startDate = endDate.AddMonths(-(months - 1));

        // Para transações de cartão usamos o mês/ano da fatura (statement);
        // para as demais, usamos o mês/ano da data da transação.
        var transactions = await (
            from t in _context.Transactions
            from s in _context.CreditCardStatements
                .Where(s => t.StatementId != null && s.Id == t.StatementId)
                .DefaultIfEmpty()
            where t.HouseholdId == householdId
               && ((t.CreditCardId == null && t.Date.Value >= startDate && t.Date.Value < endDate.AddMonths(1))
                || (t.CreditCardId != null && s != null
                    && (s.ClosingYear * 12 + s.ClosingMonth) >= (startDate.Year * 12 + startDate.Month)
                    && (s.ClosingYear * 12 + s.ClosingMonth) <= (endDate.Year   * 12 + endDate.Month)))
            select new
            {
                EffectiveMonth = t.CreditCardId != null && s != null ? s.ClosingMonth : t.Date.Value.Month,
                EffectiveYear  = t.CreditCardId != null && s != null ? s.ClosingYear  : t.Date.Value.Year,
                t.Type,
                t.Frequency,
                Amount = t.Amount.Amount
            }
        ).AsNoTracking().ToListAsync();

        var result = new List<MonthlyTrendResponse>();
        for (var i = 0; i < months; i++)
        {
            var date = startDate.AddMonths(i);
            var m    = date.Month;
            var y    = date.Year;
            var bucket = transactions.Where(t => t.EffectiveMonth == m && t.EffectiveYear == y).ToList();

            var income      = bucket.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var expense     = bucket.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var fixedExp    = bucket.Where(t => t.Type == TransactionType.Expense && t.Frequency == TransactionFrequency.Fixed).Sum(t => t.Amount);
            var variableExp = bucket.Where(t => t.Type == TransactionType.Expense && t.Frequency == TransactionFrequency.Variable).Sum(t => t.Amount);

            result.Add(new MonthlyTrendResponse(
                m, y,
                $"{MonthAbbr(m)}/{y % 100:D2}",
                income, expense, fixedExp, variableExp,
                income - expense));
        }

        return result;
    }

    public async Task<TransactionsCategoryGroupResponse> GetGroupedByCategoryForMonthAsync(
        Guid householdId, int month, int year)
    {
        var transactions = await (
            from t in _context.Transactions
            from s in _context.CreditCardStatements
                .Where(s => t.StatementId != null && s.Id == t.StatementId)
                .DefaultIfEmpty()
            where t.HouseholdId == householdId
               && ((t.CreditCardId == null && t.Date.Value.Month == month && t.Date.Value.Year == year)
                || (t.CreditCardId != null && s != null && s.ClosingMonth == month && s.ClosingYear == year))
            select new
            {
                t.CategoryId,
                t.Type,
                Amount = t.Amount.Amount
            }
        ).AsNoTracking().ToListAsync();

        var categoryIds = transactions.Select(t => t.CategoryId).Where(id => id.HasValue).Distinct().ToList();
        var categoryNames = await _context.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var expenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryTotalDto(
                g.Key,
                g.Key.HasValue && categoryNames.TryGetValue(g.Key.Value, out var n) ? n : null,
                g.Sum(t => t.Amount),
                g.Count()))
            .OrderByDescending(g => g.Total)
            .ToList();

        var incomes = transactions
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryTotalDto(
                g.Key,
                g.Key.HasValue && categoryNames.TryGetValue(g.Key.Value, out var n) ? n : null,
                g.Sum(t => t.Amount),
                g.Count()))
            .OrderByDescending(g => g.Total)
            .ToList();

        return new TransactionsCategoryGroupResponse(expenses, incomes);
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetInstallmentTransactionsAsync(Guid householdId)
    {
        var results = await BuildFullTransactionQuery(householdId)
            .Where(t => t.Frequency == TransactionFrequency.Installments.ToString()
                     && t.Status != TransactionStatus.Cancelled.ToString())
            .AsNoTracking()
            .ToListAsync();

        return results.OrderBy(t => t.Date).ToList();
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static readonly string[] AvatarPalette =
    [
        "#6366f1", "#ec4899", "#14b8a6", "#f59e0b", "#10b981",
        "#3b82f6", "#a855f7", "#ef4444", "#84cc16", "#06b6d4"
    ];

    private static string GenerateAvatarColor(Guid profileId)
    {
        var hash = Math.Abs(profileId.GetHashCode());
        return AvatarPalette[hash % AvatarPalette.Length];
    }

    private static string MonthAbbr(int month) => month switch
    {
        1 => "Jan", 2 => "Feb", 3 => "Mar", 4 => "Apr",
        5 => "May", 6 => "Jun", 7 => "Jul", 8 => "Aug",
        9 => "Sep", 10 => "Oct", 11 => "Nov", _ => "Dec"
    };
}
