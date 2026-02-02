using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;

    public AccountsController(IAccountService service)
    {
        _service = service;
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
        return CreatedAtAction(nameof(GetAll), new { id }, null);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        var accounts = await _service.GetByUserAsync(userId);
        return Ok(accounts);
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
        return NoContent();
    }

    [HttpDelete("{accountId}")]
    public async Task<IActionResult> Delete(Guid accountId)
    {
        var userId = UserContext.GetUserId(User);

        var command = new DeleteAccountCommand(userId, accountId);
        await _service.DeleteAsync(command);

        return NoContent();
    }
}
