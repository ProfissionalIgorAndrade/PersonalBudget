using PersonalBudget.Application.DTOs.Simulator;

namespace PersonalBudget.Application.Interfaces;

public interface ISimulatorService
{
    Task<ScenarioComparisonResponse> CalculateAsync(SimulateScenarioCommand command);
}
