public record UpdateAccountCommand(
    Guid UserId,
    Guid AccountId,
    Bank Bank,
    string Agency,
    string AccountNumber
);