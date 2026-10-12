public class LoginUserResponse
{
    public Guid? UserId { get; }
    public string Token { get; }
    public string RefreshToken { get; }

    private LoginUserResponse(Guid? userId, string token = "", string refreshToken = "")
    {
        UserId = userId;
        Token = token;
        RefreshToken = refreshToken;
    }

    public static LoginUserResponse Ok(Guid userId, string token, string refreshToken)
        => new(userId, token, refreshToken);
}
