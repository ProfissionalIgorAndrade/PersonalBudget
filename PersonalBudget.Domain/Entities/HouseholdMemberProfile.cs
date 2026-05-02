public class HouseholdMemberProfile
{
    public Guid Id { get; private set; }
    public Guid HouseholdId { get; private set; }
    public HouseholdMemberProfileKind Kind { get; private set; }
    public string DisplayName { get; private set; } = null!;
    /// <summary>Preenchido quando <see cref="Kind"/> é <see cref="HouseholdMemberProfileKind.LinkedUser"/>.</summary>
    public Guid? UserId { get; private set; }
    public int SortOrder { get; private set; }

    protected HouseholdMemberProfile() { }

    private HouseholdMemberProfile(
        Guid householdId,
        HouseholdMemberProfileKind kind,
        string displayName,
        Guid? userId,
        int sortOrder)
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
    }

    public static HouseholdMemberProfile CreateLinkedUser(
        Guid householdId,
        Guid userId,
        string displayName,
        int sortOrder = 0)
        => new(householdId, HouseholdMemberProfileKind.LinkedUser, displayName, userId, sortOrder);

    public static HouseholdMemberProfile CreateJoint(string displayName, Guid householdId, int sortOrder = 0)
        => new(householdId, HouseholdMemberProfileKind.Joint, displayName, null, sortOrder);

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
