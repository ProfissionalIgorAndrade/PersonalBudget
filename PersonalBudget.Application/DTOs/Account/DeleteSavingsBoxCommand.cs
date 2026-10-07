public record DeleteSavingsBoxCommand(
    Guid HouseholdId,
    Guid UserId,
    Guid AccountId,
    string? Reason,
    Guid? DestinationAccountId = null);
