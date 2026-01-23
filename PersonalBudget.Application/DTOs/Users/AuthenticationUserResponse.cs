public class AuthenticationUserResponse
{
    public Guid? UserId { get; }
    public string Token { get; }

    private AuthenticationUserResponse(Guid? userId, string token = "")
    {
        UserId = userId;
        Token = token;
    }

    public static AuthenticationUserResponse Ok(Guid userId, string token)
        => new(userId, token);
}