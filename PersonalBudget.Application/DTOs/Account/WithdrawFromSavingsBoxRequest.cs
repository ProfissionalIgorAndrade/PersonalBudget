public record WithdrawFromSavingsBoxRequest(
    decimal Amount,
    /// <summary>Motivo do resgate. Opcional; vira a observação do lançamento.</summary>
    string? Reason = null);
