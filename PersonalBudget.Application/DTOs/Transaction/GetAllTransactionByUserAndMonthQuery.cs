public record GetAllTransactionByUserAndMonthQuery(
    Guid UserId,
    int Month,
    int Year,
    int Page
);