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
    Guid AttributionProfileId,
    string CorrespondentDisplayName
);
