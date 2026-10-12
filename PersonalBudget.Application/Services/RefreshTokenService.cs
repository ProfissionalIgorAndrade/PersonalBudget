using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

public class RefreshTokenService : IRefreshTokenService
{
    private const int TokenSizeInBytes = 32;

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly RefreshTokenSettings _settings;

    public RefreshTokenService(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        RefreshTokenSettings settings)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _settings = settings;
    }

    public async Task<string> IssueAsync(Guid userId)
    {
        var (rawToken, token) = NewToken(userId, DateTime.UtcNow);

        await _refreshTokenRepository.AddAsync(token);

        return rawToken;
    }

    public async Task<RefreshTokenRotation?> RotateAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var current = await _refreshTokenRepository.GetByHashAsync(HashToken(refreshToken));
        if (current is null)
            return null;

        var now = DateTime.UtcNow;

        if (current.IsRevoked)
        {
            // A token that was already exchanged is being presented again, so
            // it leaked and the holder of the newer tokens may not be the owner.
            await RevokeAllAsync(current.UserId);
            return null;
        }

        if (current.IsExpired(now))
            return null;

        var user = await _userRepository.GetByIdAsync(current.UserId);
        if (user is null || !user.IsActive)
        {
            await RevokeAllAsync(current.UserId);
            return null;
        }

        var (rawToken, replacement) = NewToken(current.UserId, now);

        // Tracked entity: the revocation is saved together with the new token.
        current.Revoke(now, replacement.Id);
        await _refreshTokenRepository.AddAsync(replacement);

        return new RefreshTokenRotation(current.UserId, rawToken);
    }

    public async Task RevokeAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var token = await _refreshTokenRepository.GetByHashAsync(HashToken(refreshToken));
        if (token is null || token.IsRevoked)
            return;

        token.Revoke(DateTime.UtcNow);
        await _refreshTokenRepository.SaveChangesAsync();
    }

    public async Task RevokeAllAsync(Guid userId)
    {
        var tokens = await _refreshTokenRepository.GetNotRevokedByUserAsync(userId);
        if (tokens.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
            token.Revoke(now);

        await _refreshTokenRepository.SaveChangesAsync();
    }

    /// <summary>
    /// The token is 256 random bits, so a plain SHA-256 is enough to keep the
    /// stored value useless to anyone who reads the table.
    /// </summary>
    public static string HashToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private (string RawToken, RefreshToken Token) NewToken(Guid userId, DateTime now)
    {
        var rawToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
        var token = RefreshToken.Create(userId, HashToken(rawToken), now, now.AddDays(_settings.ExpiresInDays));

        return (rawToken, token);
    }
}
