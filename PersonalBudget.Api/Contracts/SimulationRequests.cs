using PersonalBudget.Application.DTOs.Simulations;

namespace PersonalBudget.Api.Contracts;

/// <summary>Corpo de POST /api/simulations e PUT /api/simulations/{id}; também cada item de /import.</summary>
public record SimulationRequest(
    string? Description,
    SimulationType Type,
    SimulationMode Mode,
    /// <summary>Mês de início "yyyy-MM".</summary>
    string? StartMonth,
    decimal Amount,
    /// <summary>Omitido = PerInstallment.</summary>
    SimulationAmountKind? AmountKind,
    /// <summary>Obrigatório (1 a 120) em Installment; ignorado nos demais modos.</summary>
    int? Installments,
    /// <summary>Só vale em Monthly: 0 a 120; null/0 = até o fim do horizonte.</summary>
    int? Months
)
{
    public SimulationInput ToInput() =>
        new(Description, Type, Mode, StartMonth, Amount, AmountKind, Installments, Months);
}

/// <summary>Corpo de POST /api/simulations/import.</summary>
public record ImportSimulationsRequest(IReadOnlyList<SimulationRequest?>? Simulations);
