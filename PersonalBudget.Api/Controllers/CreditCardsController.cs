using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api;
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
    private readonly ICreditCardImportService _importService;
    private readonly IActiveHouseholdResolver _householdResolver;

    public CreditCardsController(
        ICreditCardService service,
        ICreditCardStatementService statementService,
        ICreditCardImportService importService,
        IActiveHouseholdResolver householdResolver)
    {
        _service = service;
        _statementService = statementService;
        _importService = importService;
        _householdResolver = householdResolver;
    }

    /// <summary>
    /// Importa linhas de CSV para a fatura (mês/ano) do cartão, ignorando as que já existem.
    /// Com DryRun, apenas classifica cada linha como nova ou existente.
    /// </summary>
    [HttpPost("{creditCardId}/statement/import")]
    public async Task<IActionResult> ImportStatement(
        Guid creditCardId,
        [FromBody] ImportStatementRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new ImportStatementCommand(
            userId,
            householdId,
            creditCardId,
            request.StatementMonth,
            request.StatementYear,
            request.DryRun,
            request.Rows
        );
        var result = await _importService.ImportStatementAsync(command);

        var message = request.DryRun
            ? $"{result.Rows.Count - result.Skipped} nova(s), {result.Skipped} já existente(s)."
            : $"{result.Created} criado(s), {result.Skipped} ignorado(s).";
        return Ok(ApiResponse<ImportStatementResult>.Ok(result, message));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCreditCardRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new CreateCreditCardCommand(
            userId,
            householdId,
            request.Name,
            request.Limit,
            request.DueDay,
            request.Color,
            request.MemberId
        );

        var creditCardId = await _service.CreateAsync(command);

        return CreatedAtAction(nameof(GetAll), new { id = creditCardId }, ApiResponse<object>.Ok(new { Id = creditCardId }, "Cartão criado."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var cards = await _service.GetAllAsync(householdId);
        return Ok(ApiResponse<object>.Ok(cards));
    }

    [HttpGet("{creditCardId}/statement")]
    public async Task<IActionResult> GetStatement(
        Guid creditCardId,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int? page = null)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        if (page is null)
        {
            var statement = await _statementService.GetStatementWithTransactionsAsync(householdId, creditCardId, month, year);
            if (statement is null)
                return NotFound(ApiResponse<object?>.Fail("Cartão não encontrado ou fatura inexistente para o mês/ano informado."));
            return Ok(ApiResponse<object>.Ok(statement));
        }

        if (page < 1)
            return BadRequest(ApiResponse<object?>.Fail("Page must be at least 1."));

        var pagedStatement = await _statementService.GetStatementWithTransactionsPagedAsync(
            householdId,
            creditCardId,
            month,
            year,
            page.Value,
            15);

        if (pagedStatement is null)
            return NotFound(ApiResponse<object?>.Fail("Cartão não encontrado ou fatura inexistente para o mês/ano informado."));

        return Ok(ApiResponse<object>.Ok(pagedStatement));
    }

    [HttpGet("{creditCardId}/statement/{statementId:guid}")]
    public async Task<IActionResult> GetStatementById(
        Guid creditCardId,
        Guid statementId,
        [FromQuery] int? page = null)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        if (page is null)
        {
            var statement = await _statementService.GetStatementWithTransactionsByIdAsync(householdId, creditCardId, statementId);
            if (statement is null)
                return NotFound(ApiResponse<object?>.Fail("Fatura não encontrada."));
            return Ok(ApiResponse<object>.Ok(statement));
        }

        if (page < 1)
            return BadRequest(ApiResponse<object?>.Fail("Page must be at least 1."));

        var pagedStatement = await _statementService.GetStatementWithTransactionsByIdPagedAsync(
            householdId,
            creditCardId,
            statementId,
            page.Value,
            15);

        if (pagedStatement is null)
            return NotFound(ApiResponse<object?>.Fail("Fatura não encontrada."));

        return Ok(ApiResponse<object>.Ok(pagedStatement));
    }

    [HttpPatch("{creditCardId}/statement/{statementId:guid}/reviewed")]
    public async Task<IActionResult> SetStatementReviewed(
        Guid creditCardId,
        Guid statementId,
        [FromBody] SetReviewedRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        await _statementService.SetReviewedAsync(householdId, creditCardId, statementId, request.Reviewed);

        return Ok(ApiResponse<object?>.Ok(null, "Lançamentos da fatura atualizados."));
    }

    [HttpPut("{creditCardId}")]
    public async Task<IActionResult> Update(
        Guid creditCardId,
        [FromBody] UpdateCreditCardRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new UpdateCreditCardCommand(
            userId,
            householdId,
            creditCardId,
            request.Name,
            request.Limit,
            request.DueDay,
            request.Color,
            request.MemberId
        );

        await _service.UpdateAsync(command);
        return Ok(ApiResponse<object?>.Ok(null, "Cartão atualizado."));
    }

    [HttpDelete("{creditCardId}")]
    public async Task<IActionResult> Delete(Guid creditCardId)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new DeleteCreditCardCommand(userId, householdId, creditCardId);
        await _service.DeleteAsync(command);
        return Ok(ApiResponse<object?>.Ok(null, "Cartão excluído."));
    }
}
