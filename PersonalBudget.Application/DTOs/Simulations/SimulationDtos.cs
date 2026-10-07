namespace PersonalBudget.Application.DTOs.Simulations;

/// <summary>Dados editáveis de uma simulação, como chegam da API (ainda não validados).</summary>
public record SimulationInput(
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
);

public record CreateSimulationCommand(Guid UserId, Guid HouseholdId, SimulationInput Input);

public record UpdateSimulationCommand(Guid UserId, Guid HouseholdId, Guid SimulationId, SimulationInput Input);

public record DeleteSimulationCommand(Guid UserId, Guid HouseholdId, Guid SimulationId);

/// <summary>Importação em lote para o dono; tudo ou nada.</summary>
public record ImportSimulationsCommand(Guid UserId, Guid HouseholdId, IReadOnlyList<SimulationInput?> Items);

public record SimulationResponse(
    Guid Id,
    string Description,
    SimulationType Type,
    SimulationMode Mode,
    string StartMonth,
    decimal Amount,
    SimulationAmountKind AmountKind,
    int? Installments,
    int? Months,
    Guid OwnerUserId,
    string OwnerName,
    bool IsOwner,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
