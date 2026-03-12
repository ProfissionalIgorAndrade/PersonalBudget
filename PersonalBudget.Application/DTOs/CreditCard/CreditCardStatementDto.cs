public record CreditCardStatementDto(
    Guid Id,
    Guid CreditCardId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime ClosingDate,
    DateTime DueDate,
    decimal TotalAmount,
    string Status
);