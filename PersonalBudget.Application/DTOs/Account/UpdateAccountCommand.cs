public record UpdateAccountCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid AccountId,
    Bank Bank,
    string? Name,
    Guid? MemberId
);
