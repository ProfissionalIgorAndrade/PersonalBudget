namespace PersonalBudget.Application.DTOs.Household;

public record HouseholdProfileSummaryRow(
    Guid ProfileId,
    string DisplayName,
    decimal TotalExpenses,
    decimal TotalIncome,
    decimal Net,
    string? AvatarColor
);
