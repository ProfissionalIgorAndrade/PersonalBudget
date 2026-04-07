using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IActiveHouseholdResolver _householdResolver;

    public DashboardController(IDashboardService dashboardService, IActiveHouseholdResolver householdResolver)
    {
        _dashboardService = dashboardService;
        _householdResolver = householdResolver;
    }

    /// <summary>Resumo agregado do mês: receitas, despesas, parcelas, cartão, KPIs.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int month, [FromQuery] int year)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var summary = await _dashboardService.GetSummaryAsync(householdId, month, year);
        return Ok(ApiResponse<object>.Ok(summary));
    }

    /// <summary>Tendência dos últimos N meses terminando em endMonth/endYear.</summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends(
        [FromQuery] int months = 6,
        [FromQuery] int? endMonth = null,
        [FromQuery] int? endYear = null)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var now = DateTime.UtcNow;
        var trends = await _dashboardService.GetTrendsAsync(householdId, months, endMonth ?? now.Month, endYear ?? now.Year);
        return Ok(ApiResponse<object>.Ok(trends));
    }

    /// <summary>Categorias com gastos variáveis acima da média dos últimos N meses.</summary>
    [HttpGet("opportunities")]
    public async Task<IActionResult> GetOpportunities(
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int lookbackMonths = 3)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var opportunities = await _dashboardService.GetOpportunitiesAsync(householdId, month, year, lookbackMonths);
        return Ok(ApiResponse<object>.Ok(opportunities));
    }
}
