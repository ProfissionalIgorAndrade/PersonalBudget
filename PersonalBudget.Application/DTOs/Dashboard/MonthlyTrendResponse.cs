namespace PersonalBudget.Application.DTOs.Dashboard;

public record MonthlyTrendResponse(
    int Month,
    int Year,
    string Label,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal Balance
);
