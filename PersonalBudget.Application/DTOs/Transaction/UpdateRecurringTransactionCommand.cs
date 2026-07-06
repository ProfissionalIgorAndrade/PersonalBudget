namespace PersonalBudget.Application.DTOs.Transaction;

public record UpdateRecurringTransactionCommand(
    Guid HouseholdId,
    Guid TransactionId,
    decimal? Amount,
    string? Date,
    string? Description,
    Guid? CategoryId,
    string? DueDate,
    string? ExpirationDate,
    Guid? AttributionProfileId,
    RecurrenceEditMode RecurrenceEditMode);
