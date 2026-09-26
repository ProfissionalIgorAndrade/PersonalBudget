public record SetSavingsGoalCommand(Guid HouseholdId, Guid AccountId, decimal? Goal);
