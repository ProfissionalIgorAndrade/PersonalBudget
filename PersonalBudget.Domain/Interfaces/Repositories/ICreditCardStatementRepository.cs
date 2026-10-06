public interface ICreditCardStatementRepository
{
    Task AddAsync(CreditCardStatement statement);

    Task UpdateAsync(CreditCardStatement statement);

    Task<CreditCardStatement?> GetByIdAsync(Guid id);

    /// <summary>Fatura do cartão para o mês/ano informado (identidade única por cartão, mês e ano).</summary>
    Task<CreditCardStatement?> GetByCreditCardAndMonthYearAsync(Guid creditCardId, int month, int year);

    Task SaveChangesAsync();
}
