using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Application.Interfaces;

[ApiController]
[Authorize]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionCreationStrategy _transactionCreationStrategy;

    public TransactionsController(ITransactionService transactionService, ITransactionCreationStrategy transactionCreationStrategy)
    {
        _transactionService = transactionService;
        _transactionCreationStrategy = transactionCreationStrategy;
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
            AutoComplete: request.AutoComplete
        );

        var transactionId = await _transactionCreationStrategy.CreateAsync(command);

        return CreatedAtAction(nameof(GetAll), new { accountId = request.AccountId }, new
        {
            TransactionId = transactionId
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);

        var query = new GetAllTransactionByUserQuery(userId);
        var transactions = await _transactionService.GetByUserAsync(query);

        return Ok(transactions);
    }

    [HttpGet("month/{month}/year/{year}")]
    public async Task<IActionResult> GetAllByMonth(int month, int year)
    {
        var userId = UserContext.GetUserId(User);

        var query = new GetAllTransactionByUserAndMonthQuery(userId, month, year);
        var transactions = await _transactionService.GetByUserAndMonthAsync(query);

        return Ok(transactions);
    }

    /// <summary>
    /// Atualiza o status da transação. Aceita: Pending (1), Completed (2), Cancelled (4).
    /// Transações de cartão de crédito não podem ser alteradas por este endpoint.
    /// </summary>
    [HttpPatch("{transactionId}/status")]
    public async Task<IActionResult> UpdateStatus(Guid transactionId, [FromBody] UpdateTransactionStatusRequest request)
    {
        var userId = UserContext.GetUserId(User);

        var command = new UpdateTransactionStatusCommand(userId, transactionId, request.Status);
        await _transactionService.UpdateStatusAsync(command);

        return NoContent();
    }
}
