namespace PersonalBudget.Application.DTOs.Account;

public record RenameSavingsBoxCommand(Guid HouseholdId, Guid AccountId, string Name);
