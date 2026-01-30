public record DeleteAccountCommand(
    Guid UserId,
    Guid AccountId
);