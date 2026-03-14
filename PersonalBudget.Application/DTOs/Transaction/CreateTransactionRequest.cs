public record CreateTransactionRequest(
    Guid? AccountId,
    Guid? CategoryId,
    Guid? CreditCardId,
    Guid? FromAccountId,
    Guid? ToAccountId,
    TransactionType Type,
    PaymentMethod PaymentMethod,
    decimal Amount,
    string Date,
    string Description,
    bool AutoComplete,
    int? InstallmentCount,
    decimal? TotalAmount,
    string? Title
);
