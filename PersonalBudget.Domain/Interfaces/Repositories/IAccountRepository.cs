public interface IAccountRepository
{
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task<Account?> GetByIdAsync(Guid accountId);
    Task<IEnumerable<Account>> GetByHouseholdIdAsync(Guid householdId);
    Task SaveChangesAsync();
}
