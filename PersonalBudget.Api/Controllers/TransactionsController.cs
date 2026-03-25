using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.Interfaces;

[ApiController]
[Authorize]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IActiveHouseholdResolver _householdResolver;

    public TransactionsController(
        ITransactionService transactionService,
        IActiveHouseholdResolver householdResolver)
    {
        _transactionService = transactionService;
        _householdResolver = householdResolver;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransactionRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new CreateTransactionCommand(
            UserId: userId,
            HouseholdId: householdId,
            AttributionProfileId: request.AttributionProfileId,
            AccountId: request.AccountId,
            CategoryId: request.CategoryId,
            CreditCardId: request.CreditCardId,
            FromAccountId: request.FromAccountId,
            ToAccountId: request.ToAccountId,
            Type: request.Type,
            Frequency: request.Frequency,
            PaymentMethod: request.PaymentMethod,
            Amount: request.Amount,
            Date: request.Date,
            Description: request.Description,
            AutoComplete: request.AutoComplete,
            InstallmentCount: request.InstallmentCount,
            TotalAmount: request.TotalAmount,
            Title: request.Title,
            ExpirationDate: request.ExpirationDate,
            DueDate: request.DueDate,
            DueDay: request.DueDay,
            RepeatCount: request.RepeatCount
        );

        var transactionId = await _transactionService.CreateAsync(command);

        return CreatedAtAction(nameof(GetAll), new { accountId = request.AccountId },
            ApiResponse<object>.Ok(new { TransactionId = transactionId }, "Transação criada."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var query = new GetAllTransactionByHouseholdQuery(householdId);
        var transactions = await _transactionService.GetByHouseholdAsync(query);

        return Ok(ApiResponse<object>.Ok(transactions));
    }


    [HttpGet("id/{transactionId}")]
    public async Task<IActionResult> GetById(Guid transactionId)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var transaction = await _transactionService.GetByIdAsync(transactionId, householdId);

        return Ok(ApiResponse<object>.Ok(transaction));
    }

    [HttpGet("month/{month}/year/{year}")]
    public async Task<IActionResult> GetAllByMonth(int month, int year, [FromQuery] int? page = null)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        if (page is null)
        {
            var unpagedQuery = new GetAllTransactionByHouseholdAndMonthQuery(householdId, month, year, 1);
            var transactions = await _transactionService.GetByHouseholdAndMonthAsync(unpagedQuery);
            return Ok(ApiResponse<object>.Ok(transactions));
        }

        if (page < 1)
            return BadRequest(ApiResponse<object?>.Fail("Page must be at least 1."));

        var pagedQuery = new GetAllTransactionByHouseholdAndMonthQuery(householdId, month, year, page.Value);
        var result = await _transactionService.GetByHouseholdAndMonthPagedAsync(pagedQuery);

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPatch("{transactionId}/status")]
    public async Task<IActionResult> UpdateStatus(Guid transactionId, [FromBody] UpdateTransactionStatusRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new UpdateTransactionStatusCommand(householdId, transactionId, request.Status);
        await _transactionService.UpdateStatusAsync(command);

        return Ok(ApiResponse<object?>.Ok(null, "Status atualizado."));
    }

    [HttpDelete("batch")]
    public async Task<IActionResult> DeleteMany([FromBody] DeleteTransactionsRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        if (request.TransactionIds is null || request.TransactionIds.Count == 0)
            return BadRequest(ApiResponse<object?>.Fail("Informe ao menos um id de transação."));

        var command = new DeleteTransactionsCommand(householdId, request.TransactionIds);
        var result = await _transactionService.DeleteManyAsync(command);

        var message = result.DeletedCount switch
        {
            0 when result.SkippedCount > 0 => "Nenhuma transação foi excluída. Apenas transações com status diferente de Concluído podem ser excluídas.",
            0 => "Nenhuma transação encontrada para excluir.",
            _ when result.SkippedCount > 0 => $"{result.DeletedCount} transação(ões) excluída(s). {result.SkippedCount} ignorada(s) (concluídas ou não encontradas).",
            _ => $"{result.DeletedCount} transação(ões) excluída(s)."
        };

        return Ok(ApiResponse<object>.Ok(new
        {
            result.DeletedCount,
            result.SkippedCount,
            result.SkippedIds
        }, message));
    }
}
