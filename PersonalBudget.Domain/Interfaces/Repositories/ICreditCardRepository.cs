public interface ICreditCardRepository
{
    Task AddAsync(CreditCard creditCard);
    Task UpdateAsync(CreditCard creditCard);
    Task<CreditCard?> GetByIdAsync(Guid id);
    Task<CreditCard?> GetByIdWithStatementsAsync(Guid id);
    Task<IEnumerable<CreditCard>> GetByHouseholdAsync(Guid householdId);
    /// <summary>Todos os cartões do lar, inclusive inativos (migração).</summary>
    Task<IEnumerable<CreditCard>> GetAllByHouseholdAsync(Guid householdId);
    Task<IEnumerable<CreditCard>> GetAllByHouseholdAndUserAsync(Guid householdId, Guid userId);
    Task BulkUpdateAsync(IReadOnlyList<CreditCard> creditCards);
    /// <summary>Desativa (<see cref="CreditCard.IsActive"/>) todos os cartões ainda ativos vinculados à conta de débito.</summary>
    Task DeactivateByAccountIdAsync(Guid accountId);
    Task SaveChangesAsync();
}
