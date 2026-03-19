public record GetTransactionsByAccountAndMonthYearQuery(
    Guid UserId,
    Guid AccountId,
    int Month,
    int Year
);
