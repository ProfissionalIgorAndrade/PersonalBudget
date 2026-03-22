public record DeleteAccountCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid AccountId
);
