public record CreateTransactionRequest(
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    TransactionStatus Status
);
