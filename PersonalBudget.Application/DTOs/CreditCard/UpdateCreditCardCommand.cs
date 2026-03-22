public record UpdateCreditCardCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid CreditCardId,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay
);
