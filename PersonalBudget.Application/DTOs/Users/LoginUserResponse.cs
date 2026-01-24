public class LoginUserResponse
{
    public Guid? UserId { get; }
    public string Token { get; }

    private LoginUserResponse(Guid? userId, string token = "")
    {
        UserId = userId;
        Token = token;
    }

    public static LoginUserResponse Ok(Guid userId, string token)
        => new(userId, token);
}