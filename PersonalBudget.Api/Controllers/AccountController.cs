using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var command = new CreateAccountCommand(userId, request.Name, request.Balance);
        var accountId = await _accountService.CreateAccountAsync(command);

        return CreatedAtAction(
            nameof(Create),
            new { accountId },
            new
            {
                AccountId = accountId,
                Balance = request.Balance
            });
    }
}
