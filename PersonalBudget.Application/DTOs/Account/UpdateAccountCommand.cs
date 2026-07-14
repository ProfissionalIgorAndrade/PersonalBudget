public record UpdateAccountCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid AccountId,
    Bank Bank,
    string Agency,
    string AccountNumber,
    Guid? MemberId
);
