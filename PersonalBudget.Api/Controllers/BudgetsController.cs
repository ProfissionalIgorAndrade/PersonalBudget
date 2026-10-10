using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.DTOs.Budget;
using PersonalBudget.Application.Interfaces;

[ApiController]
[Authorize]
[Route("api/budgets")]
public class BudgetsController : ControllerBase
{
    private readonly ICategoryBudgetService _service;
    private readonly IActiveHouseholdResolver _householdResolver;

    public BudgetsController(ICategoryBudgetService service, IActiveHouseholdResolver householdResolver)
    {
        _service = service;
        _householdResolver = householdResolver;
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertCategoryBudgetRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var id = await _service.UpsertAsync(new UpsertCategoryBudgetCommand(
            householdId, request.CategoryId, request.Month, request.Year, request.LimitAmount));
        return Ok(ApiResponse<object>.Ok(new { Id = id }, "Orçamento salvo."));
    }

    [HttpGet]
    public async Task<IActionResult> GetByMonth([FromQuery] int month, [FromQuery] int year)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var result = await _service.GetByMonthAsync(householdId, month, year);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        await _service.DeleteAsync(householdId, id);
        return Ok(ApiResponse<object?>.Ok(null, "Orçamento removido."));
    }
}
