public record CreateAccountRequest(
    Bank Bank,
    string Agency,
    string AccountNumber,
    Guid MemberId
);
