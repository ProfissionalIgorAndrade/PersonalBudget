public class CreditCard
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid HouseholdId { get; private set; }

    /// <summary>
    /// Perfil de membro do lar a quem o cartão pertence.
    ///
    /// Distinto de UserId, que identifica quem criou. Um cartão da Andreza
    /// cadastrado pelo Igor tem UserId do Igor, e resolver o dono por ali
    /// mostrava o nome errado. Nullable para os cartões já existentes.
    /// </summary>
    public Guid? MemberId { get; private set; }
    public string Name { get; private set; }
    public decimal Limit { get; private set; }
    public int DueDay { get; private set; }
    public bool IsActive { get; private set; }
    /// <summary>CSS hex color, e.g. "#818cf8". Optional display hint for UI rendering.</summary>
    public string? Color { get; private set; }

    private CreditCard(
        Guid userId,
        Guid householdId,
        string name,
        decimal limit,
        int dueDay,
        string? color = null,
        Guid? memberId = null)
    {
        if (limit <= 0)
            throw new DomainException("O limite do cartão de crédito deve ser maior que zero.");
        if (householdId == Guid.Empty)
            throw new DomainException("Cartão deve pertencer a um lar.");
        ValidateDueDay(dueDay);

        Id = Guid.NewGuid();
        UserId = userId;
        HouseholdId = householdId;
        Name = name;
        Limit = limit;
        DueDay = dueDay;
        IsActive = true;
        Color = color;
        MemberId = memberId;
    }

    protected CreditCard() { }

    public static CreditCard Create(
        Guid userId,
        Guid householdId,
        string name,
        decimal limit,
        int dueDay,
        string? color = null,
        Guid? memberId = null)
        => new(userId, householdId, name, limit, dueDay, color, memberId);

    /// <summary>Atualiza os dados do cartão. <paramref name="memberId"/> nulo mantém o atual.</summary>
    public void Update(string name, decimal limit, int dueDay, string? color = null, Guid? memberId = null)
    {
        if (!IsActive)
            throw new DomainException("Cartão de crédito inativo não pode ser atualizado.");

        ValidateDueDay(dueDay);

        Name = name;
        Limit = limit;
        DueDay = dueDay;
        Color = color;

        if (memberId is { } mid && mid != Guid.Empty)
            MemberId = mid;
    }

    public void Deactivate()
        => IsActive = false;

    /// <summary>Transferência de titularidade do cartão no mesmo lar (ex.: fusão de perfis vinculados).</summary>
    public void ReassignUserId(Guid newUserId)
    {
        if (newUserId == Guid.Empty)
            throw new DomainException("Usuário inválido.");

        UserId = newUserId;
    }

    /// <summary>Move o cartão para outro lar (ex.: fusão ao aceitar convite).</summary>
    public void RelocateToHousehold(Guid newHouseholdId)
    {
        if (newHouseholdId == Guid.Empty)
            throw new DomainException("Lar inválido.");

        HouseholdId = newHouseholdId;
    }

    private static void ValidateDueDay(int dueDay)
    {
        if (dueDay < 1 || dueDay > 31)
            throw new DomainException("O dia de vencimento deve estar entre 1 e 31.");
    }
}
