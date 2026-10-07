using PersonalBudget.Application.DTOs.Simulations;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Application.Services;

public class SimulationService : ISimulationService
{
    private const string NotFoundMessage = "Simulação não encontrada.";
    private const string NotOwnerMessage = "Simulação não pertence ao usuário.";
    private const string FallbackOwnerName = "Membro";

    private readonly ISimulationRepository _simulations;
    private readonly IHouseholdMemberProfileRepository _profiles;
    private readonly IUserRepository _users;

    public SimulationService(
        ISimulationRepository simulations,
        IHouseholdMemberProfileRepository profiles,
        IUserRepository users)
    {
        _simulations = simulations;
        _profiles = profiles;
        _users = users;
    }

    public async Task<IReadOnlyList<SimulationResponse>> GetAllAsync(Guid userId, Guid householdId)
    {
        var simulations = await _simulations.GetByHouseholdAsync(householdId);
        if (simulations.Count == 0)
            return [];

        var ownerNames = await ResolveOwnerNamesAsync(householdId, simulations);

        return simulations
            .Select(s => new SimulationResponse(
                s.Id,
                s.Description,
                s.Type,
                s.Mode,
                s.StartMonth,
                s.Amount,
                s.AmountKind,
                s.Installments,
                s.Months,
                s.OwnerUserId,
                ownerNames[s.OwnerUserId],
                s.OwnerUserId == userId,
                s.CreatedAt,
                s.UpdatedAt))
            .ToList();
    }

    public async Task<Guid> CreateAsync(CreateSimulationCommand command)
    {
        SimulationValidator.Validate(command.Input);
        await EnsureWithinCapAsync(command.HouseholdId, command.UserId, adding: 1);

        var simulation = NewSimulation(command.UserId, command.HouseholdId, command.Input, createdAt: null);
        await _simulations.AddAsync(simulation);
        return simulation.Id;
    }

    public async Task UpdateAsync(UpdateSimulationCommand command)
    {
        var simulation = await GetOwnedAsync(command.SimulationId, command.UserId, command.HouseholdId);

        SimulationValidator.Validate(command.Input);

        var input = command.Input;
        simulation.Update(
            input.Description,
            input.Type,
            input.Mode,
            input.StartMonth,
            input.Amount,
            input.AmountKind ?? SimulationAmountKind.PerInstallment,
            input.Installments,
            input.Months);

        await _simulations.UpdateAsync(simulation);
    }

    public async Task DeleteAsync(DeleteSimulationCommand command)
    {
        var simulation = await GetOwnedAsync(command.SimulationId, command.UserId, command.HouseholdId);
        await _simulations.RemoveAsync(simulation);
    }

    public Task<int> DeleteMineAsync(Guid userId, Guid householdId)
        => _simulations.RemoveByOwnerAsync(householdId, userId);

    public async Task<IReadOnlyList<Guid>> ImportAsync(ImportSimulationsCommand command)
    {
        var items = command.Items;
        if (items.Count == 0)
            throw new DomainException("Nenhuma simulação para importar.");

        // O teto vem antes de validar item a item para não varrer uma lista gigante.
        if (items.Count > SimulationRules.MaxPerOwner)
            throw new DomainException($"No máximo {SimulationRules.MaxPerOwner} simulações por pessoa.");

        for (var i = 0; i < items.Count; i++)
            SimulationValidator.Validate(items[i], i + 1);

        await EnsureWithinCapAsync(command.HouseholdId, command.UserId, adding: items.Count);

        // Instantes crescentes (1 ms) preservam a ordem recebida na lista ordenada por criação.
        var baseTime = DateTime.UtcNow;
        var created = items
            .Select((item, i) => NewSimulation(command.UserId, command.HouseholdId, item!, baseTime.AddMilliseconds(i)))
            .ToList();

        await _simulations.AddRangeAsync(created);
        return created.Select(s => s.Id).ToList();
    }

    private static Simulation NewSimulation(Guid userId, Guid householdId, SimulationInput input, DateTime? createdAt)
        => Simulation.Create(
            householdId,
            userId,
            input.Description,
            input.Type,
            input.Mode,
            input.StartMonth,
            input.Amount,
            input.AmountKind ?? SimulationAmountKind.PerInstallment,
            input.Installments,
            input.Months,
            createdAt);

    private async Task EnsureWithinCapAsync(Guid householdId, Guid userId, int adding)
    {
        var current = await _simulations.CountByOwnerAsync(householdId, userId);
        if (current + adding > SimulationRules.MaxPerOwner)
            throw new DomainException(
                $"Limite de {SimulationRules.MaxPerOwner} simulações por pessoa atingido (você tem {current}).");
    }

    /// <summary>
    /// Busca por id conferindo o lar (inexistente ou de outro lar = "não encontrada") e exige
    /// que o usuário seja o dono (senão ApplicationException, mapeada para 403).
    /// </summary>
    private async Task<Simulation> GetOwnedAsync(Guid simulationId, Guid userId, Guid householdId)
    {
        var simulation = await _simulations.GetByIdAsync(simulationId);

        if (simulation is null || simulation.HouseholdId != householdId)
            throw new DomainException(NotFoundMessage);

        if (simulation.OwnerUserId != userId)
            throw new ApplicationException(NotOwnerMessage);

        return simulation;
    }

    /// <summary>
    /// Nome de cada dono, em um único lote: DisplayName do perfil LinkedUser no lar, depois
    /// User.Name, depois "Membro". Uma consulta de perfis e, só se faltar alguém, uma de usuários.
    /// </summary>
    private async Task<Dictionary<Guid, string>> ResolveOwnerNamesAsync(
        Guid householdId,
        IReadOnlyList<Simulation> simulations)
    {
        var ownerIds = simulations.Select(s => s.OwnerUserId).Distinct().ToList();

        var profiles = await _profiles.GetByHouseholdAsync(householdId);
        var names = new Dictionary<Guid, string>();
        foreach (var p in profiles)
        {
            if (p.Kind == HouseholdMemberProfileKind.LinkedUser
                && p.UserId is { } uid
                && !string.IsNullOrWhiteSpace(p.DisplayName))
                names[uid] = p.DisplayName;
        }

        var missing = ownerIds.Where(id => !names.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var users = await _users.GetByIdsAsync(missing);
            foreach (var u in users)
            {
                if (!string.IsNullOrWhiteSpace(u.Name))
                    names[u.Id] = u.Name.Trim();
            }
        }

        return ownerIds.ToDictionary(id => id, id => names.TryGetValue(id, out var n) ? n : FallbackOwnerName);
    }
}
