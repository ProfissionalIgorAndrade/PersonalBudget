public record CreateAccountCommand(
    Guid UserId,
    Guid HouseholdId,
    Bank Bank,
    string? Name,
    Guid MemberId
);
