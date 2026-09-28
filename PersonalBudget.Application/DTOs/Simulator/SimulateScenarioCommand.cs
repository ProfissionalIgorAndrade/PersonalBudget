namespace PersonalBudget.Application.DTOs.Simulator;

public record SimulateScenarioCommand(
    Guid HouseholdId,
    string? ScenarioName,
    int Months,
    IReadOnlyList<ScenarioImpactDto> Impacts
);

public record ScenarioImpactDto(
    string Description,
    decimal Amount,           // unit amount (per installment, or single total)
    string Type,              // "Expense" | "Income"
    string Mode,              // "Single" | "Installment" | "Monthly"
    string StartDate,         // ISO yyyy-MM-dd (the month this starts)
    int InstallmentCount      // 1 for Single, N for Installment, 0 = full window for Monthly
);
