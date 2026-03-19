using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;

[ApiController]
[Authorize]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;
    private readonly ITransactionService _transactionService;

    public AccountsController(IAccountService service, ITransactionService transactionService)
    {
        _service = service;
        _transactionService = transactionService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request)
    {
        var userId = UserContext.GetUserId(User);

        var command = new CreateAccountCommand(
            userId,
            request.Bank,
            request.Agency,
            request.AccountNumber,
            request.InitialBalance
        );

        var id = await _service.CreateAsync(command);
        return CreatedAtAction(nameof(GetAll), new { id }, ApiResponse<object>.Ok(new { Id = id }, "Conta criada."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        var accounts = await _service.GetByUserAsync(userId);
        return Ok(ApiResponse<object>.Ok(accounts));
    }

    /// <summary>Transações da conta no mês/ano; exclui <see cref="PaymentMethod.CreditCard"/>.</summary>
    [HttpGet("{accountId}/transactions")]
    public async Task<IActionResult> GetTransactionsByAccountAndMonth(
        Guid accountId,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int? page = null)
    {
        var userId = UserContext.GetUserId(User);
        var query = new GetTransactionsByAccountAndMonthYearQuery(userId, accountId, month, year);

        if (page is null)
        {
            var transactions = await _transactionService.GetByAccountAndMonthAsync(query);
            return Ok(ApiResponse<object>.Ok(transactions));
        }

        if (page < 1)
            return BadRequest(ApiResponse<object?>.Fail("Page must be at least 1."));

        var result = await _transactionService.GetByAccountAndMonthPagedAsync(query, page.Value);
        return Ok(ApiResponse<object>.Ok(result));
    }
    
    [HttpPut("{accountId}")]
    public async Task<IActionResult> Update(
        Guid accountId,
        [FromBody] UpdateAccountRequest request)
    {
        var userId = UserContext.GetUserId(User);

        var command = new UpdateAccountCommand(
            userId,
            accountId,
            request.Bank,
            request.Agency,
            request.AccountNumber
        );

        await _service.UpdateAsync(command);
        return Ok(ApiResponse<object?>.Ok(null, "Conta atualizada."));
    }

    [HttpDelete("{accountId}")]
    public async Task<IActionResult> Delete(Guid accountId)
    {
        var userId = UserContext.GetUserId(User);

        var command = new DeleteAccountCommand(userId, accountId);
        await _service.DeleteAsync(command);
        return Ok(ApiResponse<object?>.Ok(null, "Conta excluída."));
    }
}
