using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/installments")]
public class InstallmentsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IActiveHouseholdResolver _householdResolver;

    public InstallmentsController(ITransactionService transactionService, IActiveHouseholdResolver householdResolver)
    {
        _transactionService = transactionService;
        _householdResolver = householdResolver;
    }

    /// <summary>Parcelamentos ativos agrupados, com datas de vencimento até o cutoff informado.</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] string? upTo = null)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var cutoff = upTo is not null && DateTime.TryParse(upTo, out var parsed)
            ? parsed
            : DateTime.UtcNow.AddMonths(3);

        var groups = await _transactionService.GetActiveInstallmentsAsync(householdId, cutoff);
        return Ok(ApiResponse<object>.Ok(groups));
    }
}
