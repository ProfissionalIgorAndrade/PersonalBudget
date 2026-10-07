/// <summary>
/// Simulação "E se...?" salva no servidor. Pertence a um lar e tem um dono (quem criou):
/// todos os membros do lar a enxergam, só o dono edita ou apaga.
/// </summary>
public class Simulation
{
    public Guid Id { get; private set; }
    public Guid HouseholdId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Description { get; private set; } = "";
    public SimulationType Type { get; private set; }
    public SimulationMode Mode { get; private set; }
    /// <summary>Mês de início no formato "yyyy-MM".</summary>
    public string StartMonth { get; private set; } = "";
    public decimal Amount { get; private set; }
    public SimulationAmountKind AmountKind { get; private set; }
    /// <summary>Obrigatório (1 a 120) em <see cref="SimulationMode.Installment"/>; nulo nos demais modos.</summary>
    public int? Installments { get; private set; }
    /// <summary>Só em <see cref="SimulationMode.Monthly"/>: duração em meses (1 a 120). Nulo = até o fim do horizonte.</summary>
    public int? Months { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected Simulation() { }

    private Simulation(
        Guid householdId,
        Guid ownerUserId,
        string? description,
        SimulationType type,
        SimulationMode mode,
        string? startMonth,
        decimal amount,
        SimulationAmountKind amountKind,
        int? installments,
        int? months,
        DateTime createdAt)
    {
        if (householdId == Guid.Empty)
            throw new DomainException("Simulação deve pertencer a um lar.");
        if (ownerUserId == Guid.Empty)
            throw new DomainException("Usuário inválido.");

        Id = Guid.NewGuid();
        HouseholdId = householdId;
        OwnerUserId = ownerUserId;
        CreatedAt = createdAt;
        Apply(description, type, mode, startMonth, amount, amountKind, installments, months);
        UpdatedAt = createdAt;
    }

    /// <param name="createdAt">Opcional; omitido = agora (UTC). A importação em lote usa instantes crescentes para preservar a ordem.</param>
    public static Simulation Create(
        Guid householdId,
        Guid ownerUserId,
        string? description,
        SimulationType type,
        SimulationMode mode,
        string? startMonth,
        decimal amount,
        SimulationAmountKind amountKind,
        int? installments,
        int? months,
        DateTime? createdAt = null)
        => new(householdId, ownerUserId, description, type, mode, startMonth, amount, amountKind,
            installments, months, createdAt ?? DateTime.UtcNow);

    public void Update(
        string? description,
        SimulationType type,
        SimulationMode mode,
        string? startMonth,
        decimal amount,
        SimulationAmountKind amountKind,
        int? installments,
        int? months)
    {
        Apply(description, type, mode, startMonth, amount, amountKind, installments, months);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Move a simulação para outro lar (ex.: fusão ao aceitar convite). O dono não muda.</summary>
    public void RelocateToHousehold(Guid newHouseholdId)
    {
        if (newHouseholdId == Guid.Empty)
            throw new DomainException("Lar inválido.");

        HouseholdId = newHouseholdId;
    }

    /// <summary>Transfere a titularidade no mesmo lar (ex.: fusão de perfis vinculados).</summary>
    public void ReassignOwner(Guid newUserId)
    {
        if (newUserId == Guid.Empty)
            throw new DomainException("Usuário inválido.");

        OwnerUserId = newUserId;
    }

    private void Apply(
        string? description,
        SimulationType type,
        SimulationMode mode,
        string? startMonth,
        decimal amount,
        SimulationAmountKind amountKind,
        int? installments,
        int? months)
    {
        var error = SimulationRules.FirstError(description, type, mode, startMonth, amount, amountKind, installments, months);
        if (error is not null)
            throw new DomainException(char.ToUpperInvariant(error[0]) + error[1..]);

        SimulationRules.TryParseStartMonth(startMonth, out var normalizedMonth);

        Description = description?.Trim() ?? "";
        Type = type;
        Mode = mode;
        StartMonth = normalizedMonth;
        Amount = amount;
        AmountKind = amountKind;
        Installments = mode == SimulationMode.Installment ? installments : null;
        Months = mode == SimulationMode.Monthly && months is > 0 ? months : null;
    }
}
