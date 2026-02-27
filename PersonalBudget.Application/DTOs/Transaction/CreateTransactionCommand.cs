public record CreateTransactionCommand(
    Guid UserId,
    Guid? AccountId,
    Guid? CategoryId,
    Guid? CreditCardId,
    TransactionType Type,
    PaymentMethod PaymentMethod,
    decimal Amount,
    DateTime Date,
    string Description,
    bool AutoComplete // ← importante
);
