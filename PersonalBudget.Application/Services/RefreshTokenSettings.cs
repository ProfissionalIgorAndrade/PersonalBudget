public class RefreshTokenSettings
{
    public const int DefaultExpiresInDays = 30;

    public int ExpiresInDays { get; }

    public RefreshTokenSettings(int expiresInDays = DefaultExpiresInDays)
    {
        ExpiresInDays = expiresInDays > 0 ? expiresInDays : DefaultExpiresInDays;
    }
}
