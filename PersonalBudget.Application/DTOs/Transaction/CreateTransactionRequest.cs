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
    /// <summary>Mês da fatura (1-12). Obrigatório para PaymentMethod.CreditCard.</summary>
    int? StatementMonth = null,
    /// <summary>Ano da fatura. Obrigatório para PaymentMethod.CreditCard.</summary>
    int? StatementYear = null,
    /// <summary>Observações opcionais sobre o lançamento.</summary>
    string? Observations = null
);
