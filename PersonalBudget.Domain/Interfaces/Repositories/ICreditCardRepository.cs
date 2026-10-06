public interface ICreditCardRepository
{
    Task AddAsync(CreditCard creditCard);
    Task UpdateAsync(CreditCard creditCard);
    Task<CreditCard?> GetByIdAsync(Guid id);
    Task<IEnumerable<CreditCard>> GetByHouseholdAsync(Guid householdId);
    /// <summary>Todos os cartões do lar, inclusive inativos (migração).</summary>
    Task<IEnumerable<CreditCard>> GetAllByHouseholdAsync(Guid householdId);
    Task<IEnumerable<CreditCard>> GetAllByHouseholdAndUserAsync(Guid householdId, Guid userId);
    Task BulkUpdateAsync(IReadOnlyList<CreditCard> creditCards);
    Task SaveChangesAsync();
}
