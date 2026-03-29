/// <summary>Partial update: null means leave unchanged.</summary>
public record UpdateTransactionRequest(
    decimal? Amount = null,
    string? Date = null,
    string? Description = null,
    Guid? CategoryId = null,
    /// <summary>Due date (dd/MM/yyyy or ISO). Empty string clears.</summary>
    string? DueDate = null,
    /// <summary>Expiration date for fixed recurrence. Empty string clears.</summary>
    string? ExpirationDate = null,
    Guid? AttributionProfileId = null);
