public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task<Transaction?> GetByIdAsync(Guid transactionId);
    Task<IEnumerable<Transaction>> GetByIdsAsync(IEnumerable<Guid> transactionIds);
    Task DeleteManyAsync(IEnumerable<Transaction> transactions);
    Task<IEnumerable<Transaction>> GetByAccountAsync(Guid accountId);
    Task<IEnumerable<Transaction>> GetByUserAsync(Guid userId);
    Task<IEnumerable<Transaction>> GetByStatementIdAsync(Guid statementId);
    Task SaveChangesAsync();
}
