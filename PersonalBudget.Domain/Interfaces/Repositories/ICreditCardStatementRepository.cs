public interface ICreditCardStatementRepository
{
    Task AddAsync(CreditCardStatement statement);

    Task UpdateAsync(CreditCardStatement statement);

    Task<CreditCardStatement?> GetByIdAsync(Guid id);

    Task<CreditCardStatement?> GetOpenStatementForDateAsync(Guid creditCardId, DateTime date);

    Task<List<CreditCardStatement>> GetByCreditCardAsync(Guid creditCardId);

    Task SaveChangesAsync();
}