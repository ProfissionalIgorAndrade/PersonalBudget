public record DepositToSavingsBoxRequest(
    decimal Amount,
    /// <summary>Motivo do depósito. Opcional; vira a observação do lançamento.</summary>
    string? Reason = null);
