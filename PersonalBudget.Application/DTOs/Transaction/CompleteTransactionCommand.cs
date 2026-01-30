public record CompleteTransactionCommand(
    Guid UserId,
    Guid TransactionId
);
