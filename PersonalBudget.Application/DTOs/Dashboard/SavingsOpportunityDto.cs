namespace PersonalBudget.Application.DTOs.Dashboard;

public record SavingsOpportunityDto(
    Guid CategoryId,
    string CategoryName,
    decimal CurrentMonthAmount,
    decimal RollingAverage,
    decimal ExcessAmount,
    decimal ExcessPercent
);
