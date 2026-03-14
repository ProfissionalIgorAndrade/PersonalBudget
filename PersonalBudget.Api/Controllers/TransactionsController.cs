using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Application.Interfaces;
using PersonalBudget.Api.Contracts;

[ApiController]
[Authorize]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransactionRequest request)
    {
        var userId = UserContext.GetUserId(User);

        var command = new CreateTransactionCommand(
            UserId: userId,
            AccountId: request.AccountId,
            CategoryId: request.CategoryId,
            CreditCardId: request.CreditCardId,
            FromAccountId: request.FromAccountId,
            ToAccountId: request.ToAccountId,
            Type: request.Type,
            PaymentMethod: request.PaymentMethod,
            Amount: request.Amount,
            Date: request.Date,
            Description: request.Description,
            AutoComplete: request.AutoComplete,
            InstallmentCount: request.InstallmentCount,
            TotalAmount: request.TotalAmount,
            Title: request.Title
        );

        var transactionId = await _transactionService.CreateAsync(command);

        return CreatedAtAction(nameof(GetAll), new { accountId = request.AccountId },
            ApiResponse<object>.Ok(new { TransactionId = transactionId }, "Transação criada."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);

        var query = new GetAllTransactionByUserQuery(userId);
        var transactions = await _transactionService.GetByUserAsync(query);

        return Ok(ApiResponse<object>.Ok(transactions));
    }


    [HttpGet("id/{transactionId}")]
    public async Task<IActionResult> GetById(Guid transactionId)
    {
        var transaction = await _transactionService.GetByIdAsync(transactionId);

        return Ok(ApiResponse<object>.Ok(transaction));
    }

    [HttpGet("month/{month}/year/{year}")]
    public async Task<IActionResult> GetAllByMonth(int month, int year)
    {
        var userId = UserContext.GetUserId(User);

        var query = new GetAllTransactionByUserAndMonthQuery(userId, month, year);
        var transactions = await _transactionService.GetByUserAndMonthAsync(query);

        return Ok(ApiResponse<object>.Ok(transactions));
    }

    [HttpGet("creditCardId/{creditCardId}/month/{month}/year/{year}")]
    public async Task<IActionResult> GetAllByMonthAndYear(Guid creditCardId, int month, int year)
    {
        var userId = UserContext.GetUserId(User);

        var query = new GetAllTransactionByCreditCardStatementAndMonthYearQuery(userId, creditCardId, month, year);
        var transactions = await _transactionService.GetTransactionByCreditCardStatementAndMonthQuery(query);

        return Ok(ApiResponse<object>.Ok(transactions));
    }

    [HttpPatch("{transactionId}/status")]
    public async Task<IActionResult> UpdateStatus(Guid transactionId, [FromBody] UpdateTransactionStatusRequest request)
    {
        var userId = UserContext.GetUserId(User);

        var command = new UpdateTransactionStatusCommand(userId, transactionId, request.Status);
        await _transactionService.UpdateStatusAsync(command);

        return Ok(ApiResponse<object?>.Ok(null, "Status atualizado."));
    }

    [HttpPatch("paymentMethod/creditCard/statement")]
    public async Task<IActionResult> UpdateStatusToCreditCardStatement([FromBody] UpdateTransactionStatusToCreditCardStatementRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var command = new UpdateTransactionStatusToCreditCardStatementCommand(userId, request.CreditCardId, request.Month, request.Year, request.Status);
        await _transactionService.UpdateStatusToCreditCardStatementAsync(userId, command);
        return Ok(ApiResponse<object?>.Ok(null, "Status da fatura atualizado."));
    }
}
