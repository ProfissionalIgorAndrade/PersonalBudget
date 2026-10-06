public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task BulkUpdateAsync(IReadOnlyList<Transaction> transactions);
    Task<Transaction?> GetByIdAsync(Guid transactionId);
    Task<IEnumerable<Transaction>> GetByIdsAsync(IEnumerable<Guid> transactionIds);
    Task DeleteManyAsync(IEnumerable<Transaction> transactions);
    Task<IEnumerable<Transaction>> GetByAccountAsync(Guid accountId);
    Task<IEnumerable<Transaction>> GetByHouseholdAsync(Guid householdId);
    Task<IReadOnlyList<Transaction>> GetByHouseholdAndAttributionProfileAsync(
        Guid householdId,
        Guid attributionProfileId);
    Task<IEnumerable<Transaction>> GetByStatementIdAsync(Guid statementId);
    Task<IReadOnlyList<Transaction>> GetByRecurrenceIdAsync(Guid recurrenceId, Guid householdId);
    Task<Dictionary<Guid, decimal>> GetBalancesByAccountIdsAsync(IEnumerable<Guid> accountIds);
    /// <summary>
    /// Saldo por conta considerando só lançamentos com data até <paramref name="cutoffDate"/> (inclusive,
    /// por dia). Ao contrário de <see cref="GetBalancesByAccountIdsAsync"/>, ignora lançamentos futuros.
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetBalancesByAccountIdsUntilAsync(IEnumerable<Guid> accountIds, DateTime cutoffDate);
    Task SaveChangesAsync();
}
