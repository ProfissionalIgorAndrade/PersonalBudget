using System.Globalization;

/// <summary>
/// Regras de validação de uma simulação, compartilhadas entre a entidade (que lança
/// <see cref="DomainException"/>) e o validador da aplicação (que prefixa a mensagem com o
/// número e a descrição da simulação).
/// </summary>
public static class SimulationRules
{
    public const int MaxDescriptionLength = 120;
    public const int MaxInstallments = 120;
    public const int MaxMonthlyDuration = 120;
    public const int MinStartYear = 2000;
    public const int MaxStartYear = 2100;
    public const int MaxPerOwner = 50;

    /// <summary>
    /// Devolve a primeira violação (mensagem em pt-BR, iniciada em minúscula, sem prefixo)
    /// ou null quando a simulação é válida. A descrição é medida após o trim.
    /// </summary>
    public static string? FirstError(
        string? description,
        SimulationType type,
        SimulationMode mode,
        string? startMonth,
        decimal amount,
        SimulationAmountKind amountKind,
        int? installments,
        int? months)
    {
        if ((description?.Trim().Length ?? 0) > MaxDescriptionLength)
            return $"a descrição deve ter no máximo {MaxDescriptionLength} caracteres.";

        if (!Enum.IsDefined(type))
            return "tipo inválido. Use Income ou Expense.";

        if (!Enum.IsDefined(mode))
            return "modo inválido. Use Single, Installment ou Monthly.";

        if (!TryParseStartMonth(startMonth, out _))
            return "mês de início inválido. Use o formato yyyy-MM (ex.: 2026-10).";

        if (amount <= 0)
            return "o valor deve ser maior que zero.";

        if (!Enum.IsDefined(amountKind))
            return "tipo de valor inválido. Use PerInstallment ou Total.";

        if (mode == SimulationMode.Installment && installments is not (>= 1 and <= MaxInstallments))
            return $"o número de parcelas deve estar entre 1 e {MaxInstallments}.";

        if (mode == SimulationMode.Monthly && months is { } m && (m < 0 || m > MaxMonthlyDuration))
            return $"a duração mensal deve estar entre 0 (até o fim do horizonte) e {MaxMonthlyDuration} meses.";

        return null;
    }

    /// <summary>Aceita exatamente "yyyy-MM" com ano entre 2000 e 2100.</summary>
    public static bool TryParseStartMonth(string? raw, out string normalized)
    {
        normalized = "";
        if (raw is null
            || !DateTime.TryParseExact(raw, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            || d.Year < MinStartYear || d.Year > MaxStartYear)
            return false;

        normalized = d.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        return true;
    }
}
