public record CreateTransactionRequest(
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    TransactionStatus Status
);
