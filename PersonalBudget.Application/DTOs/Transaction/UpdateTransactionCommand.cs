public record UpdateTransactionCommand(
    Guid HouseholdId,
    Guid TransactionId,
    decimal? Amount,
    string? Date,
    string? Description,
    Guid? CategoryId,
    string? DueDate,
    string? ExpirationDate,
    Guid? AttributionProfileId,
    int? StatementMonth = null,
    int? StatementYear = null,
    /// <summary>Observações opcionais. Null = não alterar; string vazia = limpar.</summary>
    string? Observations = null);
