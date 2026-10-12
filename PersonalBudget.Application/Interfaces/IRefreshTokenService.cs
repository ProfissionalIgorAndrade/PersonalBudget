public interface IRefreshTokenService
{
    /// <summary>Creates a refresh token for the user and returns its raw value.</summary>
    Task<string> IssueAsync(Guid userId);

    /// <summary>
    /// Exchanges a valid refresh token for a new one. Returns null when the
    /// token is unknown, expired, revoked or belongs to an inactive user.
    /// </summary>
    Task<RefreshTokenRotation?> RotateAsync(string? refreshToken);

    Task RevokeAsync(string? refreshToken);
    Task RevokeAllAsync(Guid userId);
}

public record RefreshTokenRotation(Guid UserId, string RefreshToken);
