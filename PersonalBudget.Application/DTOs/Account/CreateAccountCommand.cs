public record CreateAccountCommand(
    Guid UserId,
    Bank Bank,
    string Agency,
    string AccountNumber,
    decimal InitialBalance
);
