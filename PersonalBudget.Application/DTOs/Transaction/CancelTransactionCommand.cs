public record CancelTransactionCommand(
    Guid UserId,
    Guid TransactionId
);
