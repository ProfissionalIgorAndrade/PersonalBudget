namespace PersonalBudget.Application.DTOs.Household;

public record HouseholdProfileSummaryRow(
    Guid ProfileId,
    string DisplayName,
    decimal TotalExpenses,
    decimal TotalIncome
);
