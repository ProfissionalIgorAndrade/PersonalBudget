using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.DTOs.Simulations;
using PersonalBudget.Application.Interfaces;

[ApiController]
[Authorize]
[Route("api/simulations")]
public class SimulationsController : ControllerBase
{
    private readonly ISimulationService _service;
    private readonly IActiveHouseholdResolver _householdResolver;

    public SimulationsController(
        ISimulationService service,
        IActiveHouseholdResolver householdResolver)
    {
        _service = service;
        _householdResolver = householdResolver;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var simulations = await _service.GetAllAsync(userId, householdId);
        return Ok(ApiResponse<object>.Ok(simulations));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SimulationRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var id = await _service.CreateAsync(new CreateSimulationCommand(userId, householdId, request.ToInput()));

        return CreatedAtAction(nameof(GetAll), new { id }, ApiResponse<object>.Ok(new { Id = id }, "Simulação criada."));
    }

    [HttpPut("{simulationId:guid}")]
    public async Task<IActionResult> Update(Guid simulationId, [FromBody] SimulationRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        await _service.UpdateAsync(new UpdateSimulationCommand(userId, householdId, simulationId, request.ToInput()));

        return Ok(ApiResponse<object?>.Ok(null, "Simulação atualizada."));
    }

    // Rota literal "mine" tem precedência sobre "{simulationId:guid}", que de qualquer forma não casa com "mine".
    [HttpDelete("mine")]
    public async Task<IActionResult> DeleteMine()
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var removed = await _service.DeleteMineAsync(userId, householdId);

        return Ok(ApiResponse<object>.Ok(new { Removed = removed }, "Suas simulações foram removidas."));
    }

    [HttpDelete("{simulationId:guid}")]
    public async Task<IActionResult> Delete(Guid simulationId)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        await _service.DeleteAsync(new DeleteSimulationCommand(userId, householdId, simulationId));

        return Ok(ApiResponse<object?>.Ok(null, "Simulação excluída."));
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportSimulationsRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var items = request.Simulations?.Select(s => s?.ToInput()).ToList()
                    ?? new List<SimulationInput?>();

        var ids = await _service.ImportAsync(new ImportSimulationsCommand(userId, householdId, items));

        return Ok(ApiResponse<object>.Ok(new { Imported = ids.Count, Ids = ids }, $"{ids.Count} simulação(ões) importada(s)."));
    }
}
