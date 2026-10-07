public record DeleteSavingsBoxRequest(
    /// <summary>Motivo da exclusão. Obrigatório; até 200 caracteres. Vira a observação dos lançamentos gerados.</summary>
    string? Reason,
    /// <summary>
    /// Caixinha que recebe o saldo. Obrigatória quando a caixinha tem saldo positivo;
    /// ignorada quando o saldo é zero.
    /// </summary>
    Guid? DestinationAccountId = null);
