public record CreateCreditCardCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid AccountId,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay
);
