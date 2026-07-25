namespace PersonalBudget.Application.DTOs.Transaction;

public record UpdateInstallmentStatementCommand(
    Guid HouseholdId,
    Guid TransactionId,
    int StatementMonth,
    int StatementYear,
    InstallmentEditMode EditMode);
