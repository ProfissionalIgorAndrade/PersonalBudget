namespace PersonalBudget.Application.DTOs.Transaction;

public record UpdateInstallmentStatementCommand(
    Guid HouseholdId,
    Guid TransactionId,
    int StatementMonth,
    int StatementYear,
    InstallmentEditMode EditMode,
    /// <summary>Group-level fields. Null means leave unchanged.</summary>
    Guid? CategoryId = null,
    Guid? AttributionProfileId = null,
    /// <summary>Observações. Null = não alterar; string vazia = limpar.</summary>
    string? Observations = null);
