/// <summary>
/// Long-lived credential that lets a client obtain a new access token without
/// asking for the password again. Only the hash of the token is stored.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public RefreshToken(Guid userId, string tokenHash, DateTime createdAt, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Refresh token deve pertencer a um usuário.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Hash do refresh token é obrigatório.");

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    protected RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime createdAt, DateTime expiresAt)
        => new(userId, tokenHash, createdAt, expiresAt);

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    public void Revoke(DateTime utcNow, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
            return;

        RevokedAt = utcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
