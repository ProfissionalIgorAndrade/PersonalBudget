using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;

[ApiController]
[Authorize]
[Route("api/credit-cards")]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _service;

    public CreditCardsController(ICreditCardService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCreditCardRequest request)
    {
        var userId = UserContext.GetUserId(User);

        var command = new CreateCreditCardCommand(
            userId,
            request.AccountId,
            request.Name,
            request.Limit,
            request.ClosingDay,
            request.DueDay
        );

        var creditCardId = await _service.CreateAsync(command);

        return CreatedAtAction(nameof(GetAll), new { id = creditCardId }, ApiResponse<object>.Ok(new { Id = creditCardId }, "Cartão criado."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        var cards = await _service.GetAllAsync(userId);
        return Ok(ApiResponse<object>.Ok(cards));
    }

    [HttpPut("{creditCardId}")]
    public async Task<IActionResult> Update(
        Guid creditCardId,
        [FromBody] UpdateCreditCardRequest request)
    {
        var userId = UserContext.GetUserId(User);

        var command = new UpdateCreditCardCommand(
            userId,
            creditCardId,
            request.Name,
            request.Limit,
            request.ClosingDay,
            request.DueDay
        );

        await _service.UpdateAsync(command);
        return Ok(ApiResponse<object?>.Ok(null, "Cartão atualizado."));
    }

    [HttpDelete("{creditCardId}")]
    public async Task<IActionResult> Delete(Guid creditCardId)
    {
        var userId = UserContext.GetUserId(User);

        var command = new DeleteCreditCardCommand(userId, creditCardId);
        await _service.DeleteAsync(command);
        return Ok(ApiResponse<object?>.Ok(null, "Cartão excluído."));
    }
}
