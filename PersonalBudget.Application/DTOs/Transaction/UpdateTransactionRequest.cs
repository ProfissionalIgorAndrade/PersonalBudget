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
    Guid? AttributionProfileId = null,
    /// <summary>Mês da fatura destino (1-12). Para mover uma transação de cartão para outra fatura.</summary>
    int? StatementMonth = null,
    /// <summary>Ano da fatura destino. Para mover uma transação de cartão para outra fatura.</summary>
    int? StatementYear = null);
