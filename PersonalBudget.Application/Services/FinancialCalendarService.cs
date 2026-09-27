using PersonalBudget.Application.DTOs.FinancialCalendar;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Application.Services;

public class FinancialCalendarService : IFinancialCalendarService
{
    private readonly ITransactionQueryRepository _queryRepo;
    private readonly IAccountService _accountService;

    public FinancialCalendarService(
        ITransactionQueryRepository queryRepo,
        IAccountService accountService)
    {
        _queryRepo      = queryRepo;
        _accountService = accountService;
    }

    public async Task<FinancialCalendarResponse> GetCalendarAsync(
        Guid householdId, DateTime from, DateTime to)
    {
        var fromUtc = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var toUtc   = DateTime.SpecifyKind(to.Date,   DateTimeKind.Utc);

        var transactions = await _queryRepo.GetByHouseholdAndDateRangeAsync(householdId, fromUtc, toUtc);
        var statements   = await _queryRepo.GetStatementsByHouseholdAndDueDateRangeAsync(householdId, fromUtc, toUtc);
        var summary      = await _accountService.GetSummaryAsync(householdId);
        var openingBalance = summary.TotalBalance;

        // Agrupa por data UTC (date-only)
        var txByDate = transactions
            .GroupBy(t => DateTime.SpecifyKind(t.Date.Date, DateTimeKind.Utc))
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<GetAllTransactionByUserResponse>)g.ToList());

        var stByDate = statements
            .GroupBy(s => DateTime.SpecifyKind(s.DueDate.Date, DateTimeKind.Utc))
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<FinancialCalendarStatementDto>)g.ToList());

        // Saldo projetado: acumula a partir do saldo real de hoje.
        // Apenas transações Pending são somadas (Completed já estão no Account.Balance).
        var today          = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var days           = new List<FinancialCalendarDayDto>();
        var runningBalance = openingBalance;

        for (var d = fromUtc; d <= toUtc; d = d.AddDays(1))
        {
            var dayTx   = txByDate.TryGetValue(d, out var dtx) ? dtx
                          : Array.Empty<GetAllTransactionByUserResponse>();
            var dayStmt = stByDate.TryGetValue(d, out var dst) ? dst
                          : Array.Empty<FinancialCalendarStatementDto>();

            decimal? projectedBalance = null;

            if (d >= today)
            {
                // Transações Pending ainda não afetaram Account.Balance:
                // somamos apenas elas para projetar o saldo.
                var pendingNet = dayTx
                    .Where(t => t.Status == TransactionStatus.Pending.ToString())
                    .Sum(t => t.Type == TransactionType.Income.ToString() ? t.Amount : -t.Amount);

                runningBalance   += pendingNet;
                projectedBalance  = runningBalance;
            }

            days.Add(new FinancialCalendarDayDto(d, dayTx, dayStmt, projectedBalance));
        }

        return new FinancialCalendarResponse(fromUtc, toUtc, openingBalance, days);
    }
}
