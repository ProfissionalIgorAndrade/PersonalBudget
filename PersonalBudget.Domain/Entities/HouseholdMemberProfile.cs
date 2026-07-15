public class HouseholdMemberProfile
{
    public Guid Id { get; private set; }
    public Guid HouseholdId { get; private set; }
    public HouseholdMemberProfileKind Kind { get; private set; }
    public string DisplayName { get; private set; } = null!;
    /// <summary>Preenchido quando <see cref="Kind"/> é <see cref="HouseholdMemberProfileKind.LinkedUser"/>.</summary>
    public Guid? UserId { get; private set; }
    public int SortOrder { get; private set; }
    /// <summary>CSS hex color, e.g. "#2dd4bf". Optional display hint for UI rendering.</summary>
    public string? Color { get; private set; }
    /// <summary>Emoji, e.g. "👤". Optional display hint for UI rendering.</summary>
    public string? Emoji { get; private set; }

    protected HouseholdMemberProfile() { }

    private HouseholdMemberProfile(
        Guid householdId,
        HouseholdMemberProfileKind kind,
        string displayName,
        Guid? userId,
        int sortOrder,
        string? color = null,
        string? emoji = null)
    {
        if (householdId == Guid.Empty)
            throw new DomainException("Lar é obrigatório.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Nome de exibição do correspondente é obrigatório.");

        if (kind == HouseholdMemberProfileKind.LinkedUser && (userId is null || userId == Guid.Empty))
            throw new DomainException("Perfil vinculado exige UserId.");
        if (kind == HouseholdMemberProfileKind.Joint && userId is not null)
            throw new DomainException("Perfil familiar compartilhado não deve ter UserId.");

        Id = Guid.NewGuid();
        HouseholdId = householdId;
        Kind = kind;
        DisplayName = displayName.Trim();
        UserId = userId;
        SortOrder = sortOrder;
        Color = color;
        Emoji = emoji;
    }

    public static HouseholdMemberProfile CreateLinkedUser(
        Guid householdId,
        Guid userId,
        string displayName,
        int sortOrder = 0,
        string? color = null,
        string? emoji = null)
        => new(householdId, HouseholdMemberProfileKind.LinkedUser, displayName, userId, sortOrder, color, emoji);

    public static HouseholdMemberProfile CreateJoint(string displayName, Guid householdId, int sortOrder = 0, string? color = null, string? emoji = null)
        => new(householdId, HouseholdMemberProfileKind.Joint, displayName, null, sortOrder, color, emoji);

    public void Update(string displayName, string? color, string? emoji)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Nome de exibição do correspondente é obrigatório.");

        DisplayName = displayName.Trim();
        Color = color;
        Emoji = emoji;
    }

    /// <summary>Move perfil compartilhado para outro lar (mantém o mesmo Id para referências de transações).</summary>
    public void RelocateToHousehold(Guid newHouseholdId, int newSortOrder)
    {
        if (newHouseholdId == Guid.Empty)
            throw new DomainException("Lar inválido.");
        if (Kind != HouseholdMemberProfileKind.Joint)
            throw new DomainException("Apenas perfis compartilhados podem ser relocados desta forma.");

        HouseholdId = newHouseholdId;
        SortOrder = newSortOrder;
    }
}
