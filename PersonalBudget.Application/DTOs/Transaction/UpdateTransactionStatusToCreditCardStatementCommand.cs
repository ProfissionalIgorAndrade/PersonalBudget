public record UpdateTransactionStatusToCreditCardStatementCommand(
    Guid UserId,
    Guid CreditCardId,
    int Month,
    int Year,
    BillStatus Status
);