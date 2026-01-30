public record UpdateAccountRequest(
    Bank Bank,
    string Agency,
    string AccountNumber
);