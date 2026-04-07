using PersonalBudget.Application.DTOs.Dashboard;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ITransactionQueryRepository _queryRepository;

    public DashboardService(ITransactionQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<DashboardSummaryResponse> GetSummaryAsync(Guid householdId, int month, int year)
        => _queryRepository.GetDashboardSummaryAsync(householdId, month, year);

    public Task<IReadOnlyList<MonthlyTrendResponse>> GetTrendsAsync(Guid householdId, int months, int endMonth, int endYear)
        => _queryRepository.GetTransactionTrendsAsync(householdId, months, endMonth, endYear);

    public async Task<IReadOnlyList<SavingsOpportunityDto>> GetOpportunitiesAsync(
        Guid householdId, int month, int year, int lookbackMonths)
    {
        // Fetch current month grouped by category
        var current = await _queryRepository.GetGroupedByCategoryForMonthAsync(householdId, month, year);
        var currentExpenses = current.Expenses.Where(e => e.CategoryId.HasValue).ToList();

        if (currentExpenses.Count == 0 || lookbackMonths <= 0)
            return [];

        // Fetch lookback months to compute rolling averages
        var endDate = new DateTime(year, month, 1).AddMonths(-1);
        var pastTrends = await _queryRepository.GetTransactionTrendsAsync(householdId, lookbackMonths, endDate.Month, endDate.Year);

        // For each lookback month, get the category breakdown of variable expenses
        var categoryAverages = new Dictionary<Guid, decimal>();
        for (var i = 0; i < lookbackMonths; i++)
        {
            var pastDate = new DateTime(year, month, 1).AddMonths(-(i + 1));
            var pastGroup = await _queryRepository.GetGroupedByCategoryForMonthAsync(householdId, pastDate.Month, pastDate.Year);
            foreach (var exp in pastGroup.Expenses.Where(e => e.CategoryId.HasValue))
            {
                if (!categoryAverages.ContainsKey(exp.CategoryId!.Value))
                    categoryAverages[exp.CategoryId.Value] = 0m;
                categoryAverages[exp.CategoryId.Value] += exp.Total;
            }
        }

        // Average
        foreach (var key in categoryAverages.Keys.ToList())
            categoryAverages[key] = Math.Round(categoryAverages[key] / lookbackMonths, 2);

        // Find categories where current > rollingAvg * 1.20
        const decimal threshold = 0.20m;
        var opportunities = new List<SavingsOpportunityDto>();
        foreach (var exp in currentExpenses)
        {
            if (!categoryAverages.TryGetValue(exp.CategoryId!.Value, out var avg) || avg <= 0)
                continue;

            var excess = exp.Total - avg;
            if (excess <= 0)
                continue;

            var excessPercent = Math.Round(excess / avg * 100, 2);
            if (excessPercent < threshold * 100)
                continue;

            opportunities.Add(new SavingsOpportunityDto(
                exp.CategoryId.Value,
                exp.CategoryName ?? string.Empty,
                exp.Total,
                avg,
                Math.Round(excess, 2),
                excessPercent));
        }

        return opportunities.OrderByDescending(o => o.ExcessAmount).ToList();
    }
}
