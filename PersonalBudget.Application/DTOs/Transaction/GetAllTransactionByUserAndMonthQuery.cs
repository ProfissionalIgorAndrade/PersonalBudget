public record GetAllTransactionByHouseholdAndMonthQuery(
    Guid HouseholdId,
    int Month,
    int Year,
    int Page
);
