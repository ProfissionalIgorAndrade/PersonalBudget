public record GetTransactionsByAccountAndMonthYearQuery(
    Guid HouseholdId,
    Guid AccountId,
    int Month,
    int Year,
    TransactionFrequency? Frequency = null
);
