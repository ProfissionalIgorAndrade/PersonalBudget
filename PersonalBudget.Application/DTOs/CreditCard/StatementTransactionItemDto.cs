namespace PersonalBudget.Application.DTOs.CreditCard;

public record StatementTransactionItemDto(
    Guid Id,
    DateTime Date,
    DateTime? DueDate,
    string Description,
    decimal Amount,
    Guid? CategoryId,
    string? CategoryName,
    string TransactionType,
    string Status,
    string Frequency,
    Guid AttributionProfileId,
    string CorrespondentDisplayName,
    /// <summary>Free-text notes on the transaction. Null when none were recorded.</summary>
    string? Observations = null
);
