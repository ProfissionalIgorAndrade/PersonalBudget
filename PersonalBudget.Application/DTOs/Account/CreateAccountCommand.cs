public record CreateAccountCommand(
    Guid UserId,
    Guid HouseholdId,
    Bank Bank,
    string Agency,
    string AccountNumber,
    decimal InitialBalance
);
