public interface ICreditCardStatementRepository
{
    Task AddAsync(CreditCardStatement statement);

    Task UpdateAsync(CreditCardStatement statement);

    Task<CreditCardStatement?> GetByIdAsync(Guid id);

    Task<CreditCardStatement?> GetOpenStatementForDateAsync(Guid creditCardId, DateTime date);

    Task<CreditCardStatement?> GetByCreditCardAndClosingMonthYearAsync(Guid creditCardId, int month, int year);

    Task<List<CreditCardStatement>> GetByCreditCardAsync(Guid creditCardId);

    Task SaveChangesAsync();
}