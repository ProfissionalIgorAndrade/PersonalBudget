using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.DTOs.CreditCard;
using PersonalBudget.Application.Interfaces;

[ApiController]
[Authorize]
[Route("api/credit-cards")]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _service;
    private readonly ICreditCardStatementService _statementService;

    public CreditCardsController(ICreditCardService service, ICreditCardStatementService statementService)
    {
        _service = service;
        _statementService = statementService;
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

    [HttpGet("{creditCardId}/statement")]
    public async Task<IActionResult> GetStatement(Guid creditCardId, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = UserContext.GetUserId(User);
        var statement = await _statementService.GetStatementWithTransactionsAsync(userId, creditCardId, month, year);
        if (statement is null)
            return NotFound(ApiResponse<object?>.Fail("Cartão não encontrado ou fatura inexistente para o mês/ano informado."));
        return Ok(ApiResponse<object>.Ok(statement));
    }

    [HttpPost("{creditCardId}/statements/{statementId}/close")]
    public async Task<IActionResult> CloseStatement(Guid creditCardId, Guid statementId)
    {
        var userId = UserContext.GetUserId(User);
        var command = new CloseStatementCommand(userId, creditCardId, statementId);
        await _statementService.CloseAsync(command);
        return Ok(ApiResponse<object?>.Ok(DateTime.Now, "Fatura marcada como fechada."));
    }

    [HttpPost("{creditCardId}/statements/{statementId}/pay")]
    public async Task<IActionResult> PayStatement(Guid creditCardId, Guid statementId)
    {
        var userId = UserContext.GetUserId(User);
        var command = new PayStatementCommand(userId, creditCardId, statementId);
        await _statementService.PayAsync(command);
        return Ok(ApiResponse<object?>.Ok(DateTime.Now, "Fatura paga com sucesso."));
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
