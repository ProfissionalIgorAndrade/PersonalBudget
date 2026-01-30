public record CreateCreditCardCommand(
    Guid UserId,
    Guid AccountId,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay
);
