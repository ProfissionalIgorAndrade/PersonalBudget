public record UpdateCreditCardCommand(
    Guid UserId,
    Guid CreditCardId,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay
);
