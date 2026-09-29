public record DepositToSavingsBoxCommand(Guid HouseholdId, Guid AccountId, decimal Amount, string? Reason = null);
