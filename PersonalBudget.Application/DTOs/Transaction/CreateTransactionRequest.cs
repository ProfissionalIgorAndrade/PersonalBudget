public record CreateTransactionRequest(
    Guid? AccountId,
    Guid? CategoryId,
    Guid? CreditCardId,
    Guid? FromAccountId,
    Guid? ToAccountId,
    TransactionType Type,
    PaymentMethod PaymentMethod,
    decimal Amount,
    DateTime Date,
    string Description,
    bool AutoComplete
);
