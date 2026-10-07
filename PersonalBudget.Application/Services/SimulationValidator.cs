using PersonalBudget.Application.DTOs.Simulations;

namespace PersonalBudget.Application.Services;

/// <summary>
/// Valida uma simulação no estilo do <c>ProjectionValidator</c>: toda falha vira
/// <see cref="DomainException"/> (400) com mensagem que diz qual simulação falhou
/// (número, quando vem de uma lista, e descrição). As regras vêm de <see cref="SimulationRules"/>.
/// </summary>
public static class SimulationValidator
{
    private const int MaxDescriptionInLabel = 40;

    /// <param name="number">Posição (1-based) na lista importada; null para criação/edição individual.</param>
    public static void Validate(SimulationInput? input, int? number = null)
    {
        if (input is null)
            throw new DomainException($"{BuildLabel(number, null)}: valor nulo.");

        var error = SimulationRules.FirstError(
            input.Description,
            input.Type,
            input.Mode,
            input.StartMonth,
            input.Amount,
            input.AmountKind ?? SimulationAmountKind.PerInstallment,
            input.Installments,
            input.Months);

        if (error is not null)
            throw new DomainException($"{BuildLabel(number, input.Description)}: {error}");
    }

    public static string BuildLabel(int? number, string? description)
    {
        var text = description?.Trim() ?? "";
        if (text.Length > MaxDescriptionInLabel)
            text = text[..MaxDescriptionInLabel] + "...";

        var label = number is { } n ? $"Simulação #{n}" : "Simulação";
        return text.Length == 0 ? label : $"{label} (\"{text}\")";
    }
}
