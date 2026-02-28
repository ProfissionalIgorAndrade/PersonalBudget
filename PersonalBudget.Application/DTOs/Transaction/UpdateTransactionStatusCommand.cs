public record UpdateTransactionStatusCommand(
    Guid UserId,
    Guid TransactionId,
    TransactionStatus Status
);
