public class AuthenticationResult
{
    public bool Success { get; }
    public Guid? UserId { get; }

    private AuthenticationResult(bool success, Guid? userId)
    {
        Success = success;
        UserId = userId;
    }

    public static AuthenticationResult Fail()
        => new(false, null);

    public static AuthenticationResult Ok(Guid userId)
        => new(true, userId);
}