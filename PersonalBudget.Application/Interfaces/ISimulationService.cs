using PersonalBudget.Application.DTOs.Simulations;

namespace PersonalBudget.Application.Interfaces;

public interface ISimulationService
{
    /// <summary>Todas as simulações do lar (de qualquer dono), com o nome do dono resolvido em lote.</summary>
    Task<IReadOnlyList<SimulationResponse>> GetAllAsync(Guid userId, Guid householdId);
    Task<Guid> CreateAsync(CreateSimulationCommand command);
    /// <summary>Só o dono edita; outro membro recebe ApplicationException "não pertence ao usuário" (403).</summary>
    Task UpdateAsync(UpdateSimulationCommand command);
    /// <summary>Só o dono apaga; outro membro recebe ApplicationException "não pertence ao usuário" (403).</summary>
    Task DeleteAsync(DeleteSimulationCommand command);
    /// <summary>Remove só as simulações do usuário no lar e devolve quantas foram removidas.</summary>
    Task<int> DeleteMineAsync(Guid userId, Guid householdId);
    /// <summary>Cria várias de uma vez para o usuário, tudo ou nada, respeitando o limite por dono. Devolve os ids na ordem recebida.</summary>
    Task<IReadOnlyList<Guid>> ImportAsync(ImportSimulationsCommand command);
}
