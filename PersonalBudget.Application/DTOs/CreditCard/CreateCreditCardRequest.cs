public record CreateCreditCardRequest(
    Guid AccountId,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay
);
