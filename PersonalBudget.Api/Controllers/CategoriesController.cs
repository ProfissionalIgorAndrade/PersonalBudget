using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var id = await _service.CreateAsync(new(userId, request.Name, request.Type));
        return CreatedAtAction(nameof(GetAll), new { id }, null);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        return Ok(await _service.GetAllAsync(userId));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryRequest request)
    {
        var userId = UserContext.GetUserId(User);
        await _service.UpdateAsync(new(userId, id, request.Name, request.Type));
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        await _service.DeleteAsync(new(userId, id));
        return NoContent();
    }
}
