namespace PersonalBudget.Api.Contracts;

public record SimulateScenarioRequest(
    string? ScenarioName,
    IReadOnlyList<ScenarioImpactRequest> Impacts
);

public record ScenarioImpactRequest(
    string Description,
    decimal Amount,
    string Type,
    string Mode,
    string StartDate,
    int InstallmentCount
);
