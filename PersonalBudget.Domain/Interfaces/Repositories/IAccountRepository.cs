public interface IAccountRepository
{
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task<Account?> GetByIdAsync(Guid accountId);
    Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId);
    Task SaveChangesAsync();
}
