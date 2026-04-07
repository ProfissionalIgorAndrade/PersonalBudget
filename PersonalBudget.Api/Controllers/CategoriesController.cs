using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.Interfaces;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;
    private readonly IActiveHouseholdResolver _householdResolver;

    public CategoriesController(ICategoryService service, IActiveHouseholdResolver householdResolver)
    {
        _service = service;
        _householdResolver = householdResolver;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var id = await _service.CreateAsync(new CreateCategoryCommand(householdId, request.Name, request.Type, request.Color, request.Icon));
        return CreatedAtAction(nameof(GetAll), new { id }, ApiResponse<object>.Ok(new { Id = id }, "Categoria criada."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        return Ok(ApiResponse<object>.Ok(await _service.GetAllAsync(householdId)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        await _service.UpdateAsync(new UpdateCategoryCommand(householdId, id, request.Name, request.Type, request.Color, request.Icon));
        return Ok(ApiResponse<object?>.Ok(null, "Categoria atualizada."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        await _service.DeleteAsync(new DeleteCategoryCommand(householdId, id));
        return Ok(ApiResponse<object?>.Ok(null, "Categoria excluída."));
    }
}
