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
    int? StatementYear = null);
