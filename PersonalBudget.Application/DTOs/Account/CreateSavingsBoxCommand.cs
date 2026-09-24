namespace PersonalBudget.Application.DTOs.Account;

public record CreateSavingsBoxCommand(Guid HouseholdId, Guid ParentAccountId, string Name);
