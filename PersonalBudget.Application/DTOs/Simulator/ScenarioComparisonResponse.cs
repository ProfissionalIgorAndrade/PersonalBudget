namespace PersonalBudget.Application.DTOs.Simulator;

public record ScenarioComparisonResponse(
    string ScenarioName,
    decimal BaseMonthlyIncome,
    decimal BaseMonthlyAccountExpense,
    decimal BaseMonthlyCardExpense,
    decimal CurrentBalance,
    IReadOnlyList<ScenarioMonthDto> Months,
    ScenarioSummaryDto Summary
);

public record ScenarioMonthDto(
    int Year,
    int Month,
    string Label,
    decimal BaseIncome,
    decimal BaseAccountExpense,
    decimal BaseCardExpense,
    decimal BaseBalance,
    decimal SimulatedIncome,
    decimal SimulatedExpense,
    decimal ScenarioBalance,
    decimal BalanceDelta
);

public record ScenarioSummaryDto(
    // Horizon totals (base projection over N months)
    decimal HorizonTotalIncome,
    decimal HorizonTotalAccountExpense,
    decimal HorizonTotalCardExpense,
    // Simulation impact
    decimal TotalSimulatedImpact,
    decimal BaseEndBalance,
    decimal ScenarioEndBalance,
    decimal EndBalanceDelta,
    bool ScenarioGoesNegative,
    int? FirstNegativeMonthIndex
);
