using PersonalBudget.Application.DTOs.Simulator;

namespace PersonalBudget.Application.Interfaces;

public interface IProjectionService
{
    /// <summary>Projeta saldo e fluxo mês a mês com os impactos informados. Não persiste nada.</summary>
    Task<ProjectionResponse> ProjectAsync(ProjectionCommand command);
}
