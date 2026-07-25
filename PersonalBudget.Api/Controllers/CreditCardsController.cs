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

    [HttpPost("{creditCardId}/import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportCsv(
        Guid creditCardId,
        IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object?>.Fail("Arquivo CSV é obrigatório."));

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) &&
            !file.ContentType.Contains("text"))
            return BadRequest(ApiResponse<object?>.Fail("Apenas arquivos CSV são aceitos."));

        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        string csvContent;
        using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
            csvContent = await reader.ReadToEndAsync();

        var command = new ImportCreditCardCsvCommand(userId, householdId, creditCardId, csvContent);
        var result = await _importService.ImportAsync(command);

        return Ok(ApiResponse<object>.Ok(new
        {
            result.Imported,
            result.Skipped,
            result.Errors
        }, $"{result.Imported} transação(ões) importada(s), {result.Skipped} ignorada(s)."));
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
            request.AccountId,
            request.Name,
            request.Limit,
            request.ClosingDay,
            request.DueDay,
            request.Color
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

    [HttpPatch("{creditCardId}/statements/{statementId}/status")]
    public async Task<IActionResult> UpdateStatementStatus(
        Guid creditCardId,
        Guid statementId,
        [FromBody] UpdateStatementStatusRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var command = new UpdateStatementStatusCommand(userId, householdId, creditCardId, statementId, request.Status, request.AccountId);
        try
        {
            await _statementService.UpdateStatusAsync(command);
        }
        catch (DomainException ex)
        {
            return UnprocessableEntity(ApiResponse<object?>.Fail(ex.Message));
        }
        return Ok(ApiResponse<object?>.Ok(DateTime.Now, "Status da fatura atualizado com sucesso."));
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
            request.ClosingDay,
            request.DueDay,
            request.Color
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
