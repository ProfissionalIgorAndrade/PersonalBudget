namespace PersonalBudget.Application.DTOs.Transaction;

public enum InstallmentEditMode
{
    /// <summary>Desloca todas as parcelas do grupo pelo mesmo delta de meses.</summary>
    All = 1,

    /// <summary>Desloca apenas a parcela selecionada e as seguintes (pelo mesmo delta de meses).</summary>
    ThisAndFuture = 2
}
