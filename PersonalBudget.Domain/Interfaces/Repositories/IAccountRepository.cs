public interface IAccountRepository
{
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task BulkUpdateAsync(IReadOnlyList<Account> accounts);
    Task<Account?> GetByIdAsync(Guid accountId);
    Task<IEnumerable<Account>> GetByHouseholdIdAsync(Guid householdId);
    /// <summary>Todas as contas do lar, inclusive inativas (migração / auditoria).</summary>
    Task<IEnumerable<Account>> GetAllByHouseholdIdAsync(Guid householdId);
    Task<IEnumerable<Account>> GetAllByHouseholdAndUserAsync(Guid householdId, Guid userId);
    Task SaveChangesAsync();
}
