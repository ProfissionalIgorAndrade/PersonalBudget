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
    /// <summary>Data de vencimento (dd/MM/yyyy ou ISO); opcional.</summary>
    string? DueDate = null,
    int? DueDay = null,
    int? RepeatCount = null,
    /// <summary>Correspondente (perfil). Opcional: padrão = perfil vinculado ao usuário.</summary>
    Guid? AttributionProfileId = null,
    /// <summary>Optional initial status; when omitted, behavior follows AutoComplete and payment method rules.</summary>
    TransactionStatus? Status = null
);
