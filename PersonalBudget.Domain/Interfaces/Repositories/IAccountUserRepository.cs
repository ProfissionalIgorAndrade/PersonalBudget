public interface IAccountUserRepository
{
    Task AddAsync(AccountUser accountUser);
    Task<bool> ExistsAsync(Guid accountId, Guid userId);
}
