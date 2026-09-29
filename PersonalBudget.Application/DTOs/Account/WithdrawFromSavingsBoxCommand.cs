public record WithdrawFromSavingsBoxCommand(Guid HouseholdId, Guid AccountId, decimal Amount, string? Reason = null);
