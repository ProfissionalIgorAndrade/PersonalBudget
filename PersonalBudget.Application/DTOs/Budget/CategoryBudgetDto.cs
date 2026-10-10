namespace PersonalBudget.Application.DTOs.Budget;

public record CategoryBudgetDto(
    Guid Id,
    Guid CategoryId,
    int Month,
    int Year,
    decimal LimitAmount,
    decimal SpentAmount
);
