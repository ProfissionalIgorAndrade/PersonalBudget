public interface ISavingsBoxEventRepository
{
    Task AddAsync(SavingsBoxEvent savingsBoxEvent);
    /// <summary>Eventos do lar, do mais recente para o mais antigo (desempate por Id).</summary>
    Task<IReadOnlyList<SavingsBoxEvent>> ListByHouseholdAsync(Guid householdId);
}
