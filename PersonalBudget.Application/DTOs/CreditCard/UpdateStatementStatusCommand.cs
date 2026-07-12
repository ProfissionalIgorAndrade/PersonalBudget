public record UpdateStatementStatusCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid CreditCardId,
    Guid StatementId,
    BillStatus Status,
    Guid? AccountId);
