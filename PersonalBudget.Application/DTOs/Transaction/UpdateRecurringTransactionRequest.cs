namespace PersonalBudget.Application.DTOs.Transaction;

/// <summary>
/// Atualização parcial de transação de série recorrente ou parcelamento.
/// Campos nulos não são alterados. RecurrenceEditMode define o escopo da edição.
/// </summary>
public record UpdateRecurringTransactionRequest(
    decimal? Amount = null,
    string? Date = null,
    string? Description = null,
    Guid? CategoryId = null,
    string? DueDate = null,
    string? ExpirationDate = null,
    Guid? AttributionProfileId = null,
    RecurrenceEditMode RecurrenceEditMode = RecurrenceEditMode.OnlyThis);
