public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task<IReadOnlyList<RefreshToken>> GetNotRevokedByUserAsync(Guid userId);
    Task SaveChangesAsync();
}
