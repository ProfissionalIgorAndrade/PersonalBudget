public record CreateTransactionRequest(
    Guid? AccountId,
    Guid? CategoryId,
    Guid? CreditCardId,
    Guid? FromAccountId,
    Guid? ToAccountId,
    TransactionType Type,
    TransactionFrequency Frequency,
    PaymentMethod PaymentMethod,
    decimal Amount,
    string Date,
    string Description,
    bool AutoComplete,
    int? InstallmentCount,
    decimal? TotalAmount,
    string? Title,
    string? ExpirationDate = null,
    int? DueDay = null,
    int? RepeatCount = null,
    /// <summary>Correspondente (perfil). Opcional: padrão = perfil vinculado ao usuário.</summary>
    Guid? AttributionProfileId = null
);
