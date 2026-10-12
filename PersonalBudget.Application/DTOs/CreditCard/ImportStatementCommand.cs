public record ImportStatementCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid CreditCardId,
    int StatementMonth,
    int StatementYear,
    bool DryRun,
    IReadOnlyList<ImportStatementRowRequest> Rows
);
