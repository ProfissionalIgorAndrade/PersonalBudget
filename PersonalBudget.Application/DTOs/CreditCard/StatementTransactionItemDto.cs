namespace PersonalBudget.Application.DTOs.CreditCard;

public record StatementTransactionItemDto(
    Guid Id,
    DateTime Date,
    string Description,
    decimal Amount,
    Guid? CategoryId,
    string? CategoryName,
    string TransactionType,
    string Status
);
