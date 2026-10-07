public interface ISimulationRepository
{
    /// <summary>Simulações do lar, ordenadas por criação e depois por Id (ordem estável entre aparelhos).</summary>
    Task<IReadOnlyList<Simulation>> GetByHouseholdAsync(Guid householdId);
    Task<Simulation?> GetByIdAsync(Guid id);
    Task<int> CountByOwnerAsync(Guid householdId, Guid ownerUserId);
    Task AddAsync(Simulation simulation);
    /// <summary>Insere todas num único SaveChanges (uma transação): tudo ou nada.</summary>
    Task AddRangeAsync(IReadOnlyList<Simulation> simulations);
    Task UpdateAsync(Simulation simulation);
    Task RemoveAsync(Simulation simulation);
    /// <summary>Remove as simulações de um dono no lar e devolve quantas foram removidas.</summary>
    Task<int> RemoveByOwnerAsync(Guid householdId, Guid ownerUserId);
    /// <summary>Todas as simulações do lar, para realocação ao aceitar convite.</summary>
    Task<IReadOnlyList<Simulation>> GetAllByHouseholdAsync(Guid householdId);
    Task<IReadOnlyList<Simulation>> GetAllByHouseholdAndOwnerAsync(Guid householdId, Guid ownerUserId);
    Task BulkUpdateAsync(IReadOnlyList<Simulation> simulations);
    Task SaveChangesAsync();
}
