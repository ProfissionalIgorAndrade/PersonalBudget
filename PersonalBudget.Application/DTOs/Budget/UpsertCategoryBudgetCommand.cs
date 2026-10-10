namespace PersonalBudget.Application.DTOs.Budget;

public record UpsertCategoryBudgetCommand(
    Guid HouseholdId,
    Guid CategoryId,
    int Month,
    int Year,
    decimal LimitAmount
);
