public record UpdateTransactionStatusCommand(
    Guid HouseholdId,
    Guid TransactionId,
    TransactionStatus Status
);
