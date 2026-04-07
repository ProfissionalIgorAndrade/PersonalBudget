using PersonalBudget.Application.DTOs.Dashboard;

namespace PersonalBudget.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(Guid householdId, int month, int year);
    Task<IReadOnlyList<MonthlyTrendResponse>> GetTrendsAsync(Guid householdId, int months, int endMonth, int endYear);
    Task<IReadOnlyList<SavingsOpportunityDto>> GetOpportunitiesAsync(Guid householdId, int month, int year, int lookbackMonths);
}
