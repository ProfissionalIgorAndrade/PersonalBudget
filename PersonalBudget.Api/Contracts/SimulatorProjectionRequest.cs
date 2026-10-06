using PersonalBudget.Application.DTOs.Simulator;

namespace PersonalBudget.Api.Contracts;

/// <summary>Pedido de POST /api/simulator/projection.</summary>
public record SimulatorProjectionRequest(
    /// <summary>Data de referência "yyyy-MM-dd". Omitida = data do servidor em UTC-3.</summary>
    string? Today,
    /// <summary>Horizonte em meses, 1 a 24 (inclui o mês de referência).</summary>
    int Months,
    /// <summary>Até 50 impactos. Omitido = sem impactos.</summary>
    IReadOnlyList<SimulatorProjectionImpactRequest>? Impacts
);

public record SimulatorProjectionImpactRequest(
    string? Id,
    string? Description,
    ImpactType Type,
    ImpactMode Mode,
    /// <summary>Mês de início "yyyy-MM".</summary>
    string? StartMonth,
    decimal Amount,
    /// <summary>Só vale em Installment; omitido = PerInstallment.</summary>
    ImpactAmountKind? AmountKind,
    /// <summary>Só vale em Installment: 1 a 120.</summary>
    int? Installments,
    /// <summary>Só vale em Monthly: duração em meses; null/0 = até o fim do horizonte.</summary>
    int? Months
);
