public record CreateTransactionCommand(
    Guid UserId,
    Guid? AccountId,
    Guid? CategoryId,
    Guid? CreditCardId,
    Guid? FromAccountId,
    Guid? ToAccountId,
    TransactionType? Type,
    PaymentMethod PaymentMethod,
    decimal Amount,
    string Date, // Formato Brasil dd/MM/yyyy (ex: 02/03/2026)
    string Description,
    bool AutoComplete // ← importante
);
