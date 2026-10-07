public record CreateSavingsBoxCommand(Guid HouseholdId, Guid UserId, Guid ParentAccountId, string Name);
