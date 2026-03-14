using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;

namespace PersonalBudget.Api.Controllers;

[ApiController]
[Route("api/authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public AuthenticationController(IUserService userService, JwtTokenGenerator jwtTokenGenerator)
    {
        _userService = userService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    [HttpPost("signin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] SigninRequest request)
    {
        var userId = await _userService.CreateUserAsync(request);

        var token = _jwtTokenGenerator.Generate(userId);

        return Ok(ApiResponse<object>.Ok(LoginUserResponse.Ok(userId, token), "Usuário criado e autenticado."));
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Authenticate(
        [FromBody] LoginUserRequest request)
    {
        var userId = await _userService.AuthenticationUserAsync(request);

        var token = _jwtTokenGenerator.Generate(userId);

        return Ok(ApiResponse<object>.Ok(LoginUserResponse.Ok(userId, token)));
    }
}
