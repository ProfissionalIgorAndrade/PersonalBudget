namespace PersonalBudget.Application.DTOs.Transaction;

/// <summary>
/// Move as parcelas de um grupo parcelado para novos meses de fatura.
/// O delta é calculado entre a fatura atual da parcela selecionada e a nova fatura informada,
/// e esse mesmo delta é aplicado a todas as parcelas afetadas pelo <see cref="EditMode"/>.
/// </summary>
public record UpdateInstallmentStatementRequest(
    /// <summary>Mês da nova fatura para a parcela selecionada (1-12).</summary>
    int StatementMonth,
    /// <summary>Ano da nova fatura para a parcela selecionada.</summary>
    int StatementYear,
    /// <summary>Define quais parcelas serão deslocadas: todas ou esta e as seguintes.</summary>
    InstallmentEditMode EditMode,
    /// <summary>Categoria aplicada às parcelas afetadas. Null não altera.</summary>
    Guid? CategoryId = null,
    /// <summary>Correspondente aplicado às parcelas afetadas. Null não altera.</summary>
    Guid? AttributionProfileId = null,
    /// <summary>Observações aplicadas às parcelas afetadas. Null não altera; string vazia limpa.</summary>
    string? Observations = null);
