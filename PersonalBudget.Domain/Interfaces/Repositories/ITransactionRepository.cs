public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task<Transaction?> GetByIdAsync(Guid transactionId);
    Task<IEnumerable<Transaction>> GetByAccountAsync(Guid accountId);
}
