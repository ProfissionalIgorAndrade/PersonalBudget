public record GetAllTransactionByCreditCardStatementAndMonthYearQuery(
    Guid HouseholdId,
    Guid CreditCardId,
    int Month,
    int Year);
