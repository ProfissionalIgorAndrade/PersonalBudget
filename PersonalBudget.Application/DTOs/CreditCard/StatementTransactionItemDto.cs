namespace PersonalBudget.Application.DTOs.CreditCard;

public record StatementTransactionItemDto(
    Guid Id,
    DateTime Date,
    string Description,
    decimal Amount,
    string? CategoryName
);
