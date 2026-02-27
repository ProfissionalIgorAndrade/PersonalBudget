public record CreateTransactionRequest(
    Guid? AccountId,
    Guid? CategoryId,
    Guid? CreditCardId,
    TransactionType Type,
    PaymentMethod PaymentMethod,
    decimal Amount,
    DateTime Date,
    string Description,
    bool AutoComplete
);
