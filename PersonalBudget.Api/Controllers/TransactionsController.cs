using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            userId,
            request.AccountId,
            request.CategoryId,
            request.CreditCardId,
            request.Type,
            request.PaymentMethod,
            request.Amount,
            request.Date,
            request.Description,
            request.AutoComplete
        );

        var transactionId = await _transactionService.CreateAsync(command);

        return CreatedAtAction(nameof(GetByAccount), new { accountId = request.AccountId }, new
        {
            TransactionId = transactionId
        });
    }

    [HttpGet("account/{accountId}")]
    public async Task<IActionResult> GetByAccount(Guid accountId)
    {
        var userId = UserContext.GetUserId(User);

        var query = new GetTransactionsByAccountQuery(userId, accountId);
        var transactions = await _transactionService.GetByAccountAsync(query);

        return Ok(transactions);
    }

    [HttpGet("grouped-by-category")]
    public async Task<IActionResult> GetByCategory()
    {
        var userId = UserContext.GetUserId(User);

        var query = new GetAllTransactionByUserQuery(userId);
        var transactions = await _transactionService.GetByUserAsync(query);
        var groupedTransactions = transactions
            .GroupBy(t => t.CategoryName)
            .Select(g => new
            {
                Category = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                Transactions = g.Select(t => new
                {
                    t.Id,
                    t.AccountId,
                    t.CategoryId,
                    t.CreditCardId,
                    t.Type,
                    t.PaymentMethod,
                    t.Amount,
                    t.Date,
                    t.Description,
                    t.Status
                })
            });

        return Ok(groupedTransactions);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);

        var query = new GetAllTransactionByUserQuery(userId);
        var transactions = await _transactionService.GetByUserAsync(query);

        return Ok(transactions);
    }
   
    [HttpPost("{transactionId}/complete")]
    public async Task<IActionResult> Complete(Guid transactionId)
    {
        var userId = UserContext.GetUserId(User);

        var command = new CompleteTransactionCommand(userId, transactionId);
        await _transactionService.CompleteAsync(command);

        return NoContent();
    }

    [HttpPost("{transactionId}/cancel")]
    public async Task<IActionResult> Cancel(Guid transactionId)
    {
        var userId = UserContext.GetUserId(User);

        var command = new CancelTransactionCommand(userId, transactionId);
        await _transactionService.CancelAsync(command);

        return NoContent();
    }
}
