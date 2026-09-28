using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.DTOs.Simulator;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/simulator")]
public class SimulatorController : ControllerBase
{
    private readonly ISimulatorService _simulatorService;
    private readonly IActiveHouseholdResolver _householdResolver;

    public SimulatorController(ISimulatorService simulatorService, IActiveHouseholdResolver householdResolver)
    {
        _simulatorService = simulatorService;
        _householdResolver = householdResolver;
    }

    /// <summary>Calcula uma projeção de cenário hipotético sem persistir dados reais.</summary>
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(
        [FromBody] SimulateScenarioRequest request,
        [FromQuery] int months = 6)
    {
        if (request.Impacts.Any(i => i.Amount <= 0))
            return BadRequest(ApiResponse<object>.Fail("Todos os impactos devem ter valor positivo."));

        if (months is < 1 or > 24)
            return BadRequest(ApiResponse<object>.Fail("O horizonte deve estar entre 1 e 24 meses."));

        var userId      = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new SimulateScenarioCommand(
            HouseholdId:  householdId,
            ScenarioName: request.ScenarioName,
            Months:       months,
            Impacts: request.Impacts.Select(i => new ScenarioImpactDto(
                i.Description, i.Amount, i.Type, i.Mode, i.StartDate, i.InstallmentCount)).ToList()
        );

        var result = await _simulatorService.CalculateAsync(command);
        return Ok(ApiResponse<ScenarioComparisonResponse>.Ok(result));
    }
}
