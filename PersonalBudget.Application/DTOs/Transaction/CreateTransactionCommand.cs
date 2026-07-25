public record CreateTransactionCommand(
    Guid UserId,
    Guid HouseholdId,
    /// <summary>Opcional: se omitido, usa o perfil vinculado ao usuário no lar.</summary>
    Guid? AttributionProfileId,
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
    /// <summary>Optional due date (dd/MM/yyyy or ISO); persisted when provided.</summary>
    string? DueDate = null,
    int? DueDay = null,
    int? RepeatCount = null,
    /// <summary>When set, overrides default status (including AutoComplete for Account).</summary>
    TransactionStatus? Status = null,
    /// <summary>Mês da fatura (1-12). Obrigatório para PaymentMethod.CreditCard.</summary>
    int? StatementMonth = null,
    /// <summary>Ano da fatura. Obrigatório para PaymentMethod.CreditCard.</summary>
    int? StatementYear = null
);
