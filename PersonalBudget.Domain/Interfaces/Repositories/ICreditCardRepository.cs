public interface ICreditCardRepository
{
    Task AddAsync(CreditCard creditCard);
    Task UpdateAsync(CreditCard creditCard);
    Task<CreditCard?> GetByIdAsync(Guid id);
    Task<IEnumerable<CreditCard>> GetByUserAsync(Guid userId);
}
