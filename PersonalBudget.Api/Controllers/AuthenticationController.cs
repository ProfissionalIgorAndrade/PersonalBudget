using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;

namespace PersonalBudget.Api.Controllers;

[ApiController]
[Route("api/authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthenticationController(
        IUserService userService,
        JwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenService refreshTokenService)
    {
        _userService = userService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("signin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] SigninRequest request)
    {
        var userId = await _userService.CreateUserAsync(request);

        var token = _jwtTokenGenerator.Generate(userId);
        var refreshToken = await _refreshTokenService.IssueAsync(userId);

        return Ok(ApiResponse<object>.Ok(LoginUserResponse.Ok(userId, token, refreshToken), "Usuário criado e autenticado."));
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Authenticate(
        [FromBody] LoginUserRequest request)
    {
        var userId = await _userService.AuthenticationUserAsync(request);

        var token = _jwtTokenGenerator.Generate(userId);
        var refreshToken = await _refreshTokenService.IssueAsync(userId);

        return Ok(ApiResponse<object>.Ok(LoginUserResponse.Ok(userId, token, refreshToken)));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var rotation = await _refreshTokenService.RotateAsync(request.RefreshToken);

        if (rotation is null)
            return Unauthorized(ApiResponse<object?>.Fail("Sessão expirada. Faça login novamente."));

        var token = _jwtTokenGenerator.Generate(rotation.UserId);

        return Ok(ApiResponse<object>.Ok(LoginUserResponse.Ok(rotation.UserId, token, rotation.RefreshToken)));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _refreshTokenService.RevokeAsync(request.RefreshToken);

        return Ok(ApiResponse<object?>.Ok(null, "Sessão encerrada."));
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = UserContext.GetUserId(User);

        await _userService.ChangePasswordAsync(userId, request);

        // Whoever knew the old password must not keep a session alive.
        await _refreshTokenService.RevokeAllAsync(userId);

        return Ok(ApiResponse<object?>.Ok(null, "Senha alterada com sucesso."));
    }
}
