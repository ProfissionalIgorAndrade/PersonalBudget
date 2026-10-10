namespace PersonalBudget.Application.DTOs.Budget;

public record UpsertCategoryBudgetRequest(
    Guid CategoryId,
    int Month,
    int Year,
    decimal LimitAmount
);
