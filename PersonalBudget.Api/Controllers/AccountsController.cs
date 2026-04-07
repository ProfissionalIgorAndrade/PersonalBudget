using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.Interfaces;

[ApiController]
[Authorize]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;
    private readonly ITransactionService _transactionService;
    private readonly IActiveHouseholdResolver _householdResolver;

    public AccountsController(
        IAccountService service,
        ITransactionService transactionService,
        IActiveHouseholdResolver householdResolver)
    {
        _service = service;
        _transactionService = transactionService;
        _householdResolver = householdResolver;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new CreateAccountCommand(
            userId,
            householdId,
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
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var accounts = await _service.GetByHouseholdAsync(householdId);
        return Ok(ApiResponse<object>.Ok(accounts));
    }

    /// <summary>Saldo total consolidado e lista de contas ativas.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var summary = await _service.GetSummaryAsync(householdId);
        return Ok(ApiResponse<object>.Ok(summary));
    }

    /// <summary>Transações da conta no mês/ano; exclui <see cref="PaymentMethod.CreditCard"/>.</summary>
    [HttpGet("{accountId}/transactions")]
    public async Task<IActionResult> GetTransactionsByAccountAndMonth(
        Guid accountId,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int? page = null,
        [FromQuery] string? frequency = null)
    {
        var userId = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        TransactionFrequency? freq = null;
        if (frequency is not null && Enum.TryParse<TransactionFrequency>(frequency, ignoreCase: true, out var parsedFreq))
            freq = parsedFreq;

        var query = new GetTransactionsByAccountAndMonthYearQuery(householdId, accountId, month, year, freq);

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
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new UpdateAccountCommand(
            userId,
            householdId,
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
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));

        var command = new DeleteAccountCommand(userId, householdId, accountId);
        await _service.DeleteAsync(command);
        return Ok(ApiResponse<object?>.Ok(null, "Conta excluída."));
    }
}
