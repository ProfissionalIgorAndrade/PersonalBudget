/// <summary>
/// Evento de histórico de uma caixinha (criação ou exclusão). Guarda o nome da caixinha
/// no momento do evento, porque a caixinha excluída fica inativa e some das listagens.
/// Sem chaves estrangeiras, como no resto do modelo.
/// </summary>
public class SavingsBoxEvent
{
    public Guid Id { get; private set; }
    public Guid HouseholdId { get; private set; }
    /// <summary>Usuário que executou a ação.</summary>
    public Guid UserId { get; private set; }
    /// <summary>Caixinha à qual o evento se refere.</summary>
    public Guid AccountId { get; private set; }
    /// <summary>Nome da caixinha na hora do evento.</summary>
    public string BoxName { get; private set; } = "";
    public SavingsBoxEventKind Kind { get; private set; }
    /// <summary>Motivo da exclusão. Obrigatório em <see cref="SavingsBoxEventKind.Deleted"/>; nulo na criação.</summary>
    public string? Reason { get; private set; }
    /// <summary>Saldo movido para outra caixinha na exclusão; 0 na criação ou quando não havia saldo.</summary>
    public decimal Amount { get; private set; }
    public Guid? DestinationAccountId { get; private set; }
    /// <summary>Nome da caixinha de destino na hora da exclusão.</summary>
    public string? DestinationName { get; private set; }
    public DateTime OccurredAt { get; private set; }

    /// <summary>Tamanho máximo de <see cref="BoxName"/> e <see cref="DestinationName"/>; igual ao nome da caixinha.</summary>
    public const int NameMaxLength = Account.NameMaxLength;

    /// <summary>Tamanho máximo de <see cref="Reason"/>.</summary>
    public const int ReasonMaxLength = 200;

    protected SavingsBoxEvent() { }

    private SavingsBoxEvent(
        Guid householdId,
        Guid userId,
        Guid accountId,
        string? boxName,
        SavingsBoxEventKind kind,
        string? reason,
        decimal amount,
        Guid? destinationAccountId,
        string? destinationName,
        DateTime occurredAt)
    {
        if (householdId == Guid.Empty)
            throw new DomainException("O evento deve pertencer a um lar.");
        if (userId == Guid.Empty)
            throw new DomainException("Usuário inválido.");
        if (accountId == Guid.Empty)
            throw new DomainException("Caixinha inválida.");
        if (amount < 0)
            throw new DomainException("O valor do evento não pode ser negativo.");

        var name = boxName?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new DomainException("A caixinha precisa de um nome.");
        if (name.Length > NameMaxLength)
            throw new DomainException($"O nome deve ter no máximo {NameMaxLength} caracteres.");

        var trimmedReason = reason?.Trim();
        if (kind == SavingsBoxEventKind.Deleted && string.IsNullOrEmpty(trimmedReason))
            throw new DomainException("Informe o motivo da exclusão da caixinha.");
        if (trimmedReason is { Length: > ReasonMaxLength })
            throw new DomainException($"O motivo deve ter no máximo {ReasonMaxLength} caracteres.");

        if (destinationAccountId == Guid.Empty)
            throw new DomainException("Caixinha de destino inválida.");

        var trimmedDestination = destinationName?.Trim();
        if (destinationAccountId.HasValue && string.IsNullOrEmpty(trimmedDestination))
            throw new DomainException("A caixinha de destino precisa de um nome.");
        if (trimmedDestination is { Length: > NameMaxLength })
            throw new DomainException($"O nome deve ter no máximo {NameMaxLength} caracteres.");

        Id = Guid.NewGuid();
        HouseholdId = householdId;
        UserId = userId;
        AccountId = accountId;
        BoxName = name;
        Kind = kind;
        Reason = string.IsNullOrEmpty(trimmedReason) ? null : trimmedReason;
        Amount = amount;
        DestinationAccountId = destinationAccountId;
        DestinationName = destinationAccountId.HasValue ? trimmedDestination : null;
        OccurredAt = occurredAt;
    }

    /// <param name="occurredAt">Opcional; omitido = agora (UTC).</param>
    public static SavingsBoxEvent Created(
        Guid householdId,
        Guid userId,
        Guid accountId,
        string boxName,
        DateTime? occurredAt = null)
        => new(householdId, userId, accountId, boxName, SavingsBoxEventKind.Created,
            reason: null, amount: 0m, destinationAccountId: null, destinationName: null,
            occurredAt ?? DateTime.UtcNow);

    /// <param name="amount">Saldo movido para o destino (0 quando a caixinha estava zerada).</param>
    /// <param name="occurredAt">Opcional; omitido = agora (UTC).</param>
    public static SavingsBoxEvent Deleted(
        Guid householdId,
        Guid userId,
        Guid accountId,
        string boxName,
        string? reason,
        decimal amount,
        Guid? destinationAccountId,
        string? destinationName,
        DateTime? occurredAt = null)
        => new(householdId, userId, accountId, boxName, SavingsBoxEventKind.Deleted,
            reason, amount, destinationAccountId, destinationName,
            occurredAt ?? DateTime.UtcNow);
}
