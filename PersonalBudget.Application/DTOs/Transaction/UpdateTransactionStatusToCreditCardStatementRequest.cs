public record UpdateTransactionStatusToCreditCardStatementRequest(
    Guid CreditCardId,
    int Month,
    int Year,
    BillStatus Status
);