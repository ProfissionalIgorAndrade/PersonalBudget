using System.Globalization;
using PersonalBudget.Application.DTOs.Simulator;

namespace PersonalBudget.Application.Services.Simulator;

/// <summary>Pedido já validado e normalizado.</summary>
public sealed record ValidatedProjection(
    DateTime Today,
    YearMonth Reference,
    int Months,
    IReadOnlyList<ImpactDefinition> Impacts);

/// <summary>
/// Valida o pedido de projeção. Toda falha vira <see cref="DomainException"/> (400), com
/// mensagem que diz qual impacto falhou (posição e descrição).
/// </summary>
public static class ProjectionValidator
{
    public const int MaxMonths = 24;
    public const int MaxImpacts = 50;
    public const int MaxDescriptionLength = 120;
    public const int MaxInstallments = 120;
    public const int MaxMonthlyDuration = 120;
    public const int MinStartYear = 2000;
    public const int MaxStartYear = 2100;

    /// <param name="defaultToday">Usado quando o pedido não traz "today" (data do servidor em UTC-3).</param>
    public static ValidatedProjection Validate(ProjectionCommand command, DateTime defaultToday)
    {
        if (command.Months is < 1 or > MaxMonths)
            throw new DomainException($"O horizonte deve estar entre 1 e {MaxMonths} meses.");

        var today = ParseToday(command.Today, defaultToday);

        var inputs = command.Impacts ?? Array.Empty<ProjectionImpactInput>();
        if (inputs.Count > MaxImpacts)
            throw new DomainException($"No máximo {MaxImpacts} impactos por projeção.");

        var impacts = new List<ImpactDefinition>(inputs.Count);
        for (var i = 0; i < inputs.Count; i++)
            impacts.Add(ValidateImpact(inputs[i], i));

        return new ValidatedProjection(today, YearMonth.FromDate(today), command.Months, impacts);
    }

    private static DateTime ParseToday(string? raw, DateTime defaultToday)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return defaultToday.Date;

        if (!DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            throw new DomainException("Data de referência inválida. Use o formato yyyy-MM-dd.");

        return parsed.Date;
    }

    private static ImpactDefinition ValidateImpact(ProjectionImpactInput? input, int index)
    {
        if (input is null)
            throw new DomainException($"Impacto #{index + 1}: valor nulo.");

        var description = input.Description?.Trim() ?? "";
        var label = description.Length == 0
            ? $"Impacto #{index + 1}"
            : $"Impacto #{index + 1} (\"{description}\")";

        if (description.Length > MaxDescriptionLength)
            throw new DomainException($"{label}: a descrição deve ter no máximo {MaxDescriptionLength} caracteres.");

        if (!Enum.IsDefined(input.Type))
            throw new DomainException($"{label}: tipo inválido. Use Income ou Expense.");

        if (!Enum.IsDefined(input.Mode))
            throw new DomainException($"{label}: modo inválido. Use Single, Installment ou Monthly.");

        if (!YearMonth.TryParse(input.StartMonth, out var start)
            || start.Year < MinStartYear || start.Year > MaxStartYear)
            throw new DomainException($"{label}: mês de início inválido. Use o formato yyyy-MM (ex.: 2026-10).");

        if (input.Amount <= 0)
            throw new DomainException($"{label}: o valor deve ser maior que zero.");

        var amountKind = input.AmountKind ?? ImpactAmountKind.PerInstallment;
        if (!Enum.IsDefined(amountKind))
            throw new DomainException($"{label}: tipo de valor inválido. Use PerInstallment ou Total.");

        var installments = 1;
        if (input.Mode == ImpactMode.Installment)
        {
            if (input.Installments is not (>= 1 and <= MaxInstallments))
                throw new DomainException($"{label}: o número de parcelas deve estar entre 1 e {MaxInstallments}.");
            installments = input.Installments.Value;
        }

        int? months = null;
        if (input.Mode == ImpactMode.Monthly && input.Months is { } m)
        {
            if (m < 0 || m > MaxMonthlyDuration)
                throw new DomainException($"{label}: a duração mensal deve estar entre 0 (até o fim do horizonte) e {MaxMonthlyDuration} meses.");
            months = m == 0 ? null : m;
        }

        var id = string.IsNullOrWhiteSpace(input.Id) ? $"impact-{index + 1}" : input.Id.Trim();

        return new ImpactDefinition(id, description, input.Type, input.Mode, start, input.Amount, amountKind, installments, months);
    }
}
