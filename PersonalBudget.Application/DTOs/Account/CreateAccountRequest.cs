public record CreateAccountRequest(
    Bank Bank,
    string Agency,
    string AccountNumber,
    decimal InitialBalance
);
