public class Account
{
    public Guid Id { get; private set; }
    /// <summary>Usuário que criou a conta (auditoria).</summary>
    public Guid UserId { get; private set; }
    /// <summary>Lar ao qual a conta pertence (visível a todos os membros).</summary>
    public Guid HouseholdId { get; private set; }
    /// <summary>Membro da família ao qual esta conta pertence.</summary>
    public Guid? MemberProfileId { get; private set; }
    public Bank Bank { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public AccountKind Kind { get; private set; } = AccountKind.Checking;

    /// <summary>
    /// Conta corrente à qual a caixinha pertence. Null para conta corrente.
    /// </summary>
    public Guid? ParentAccountId { get; private set; }

    /// <summary>
    /// Nome da caixinha ("Viagem", "Reserva"), obrigatório nela. Na conta corrente é
    /// um apelido opcional para diferenciar contas do mesmo banco; null quando ausente.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>Tamanho máximo de <see cref="Name"/>; espelha a coluna "name".</summary>
    public const int NameMaxLength = 80;

    /// <summary>
    /// Meta de quanto se quer acumular na caixinha. Null quando não há meta —
    /// nem toda caixinha precisa de uma, e zero significaria meta batida.
    /// </summary>
    public decimal? SavingsGoal { get; private set; }

    public Account(
        Guid userId,
        Guid householdId,
        Bank bank,
        string? name,
        Guid memberProfileId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("A conta deve pertencer a um usuário.");
        if (householdId == Guid.Empty)
            throw new DomainException("A conta deve pertencer a um lar.");
        if (memberProfileId == Guid.Empty)
            throw new DomainException("A conta deve estar vinculada a um membro da família.");

        Id = Guid.NewGuid();
        UserId = userId;
        HouseholdId = householdId;
        MemberProfileId = memberProfileId;
        Bank = bank;
        Name = NormalizeName(name);
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cria uma caixinha vinculada a uma conta corrente.
    ///
    /// Herda banco e membro da conta pai: uma caixinha não
    /// tem identidade bancária própria, é uma divisão do mesmo dinheiro.
    /// </summary>
    public static Account CreateSavingsBox(Account parent, string name)
    {
        if (parent is null)
            throw new DomainException("Caixinha precisa de uma conta de origem.");
        if (parent.Kind == AccountKind.Savings)
            throw new DomainException("Uma caixinha não pode pertencer a outra caixinha.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A caixinha precisa de um nome.");

        return new Account(parent.UserId, parent.HouseholdId, parent.Bank,
                           name,
                           parent.MemberProfileId!.Value)
        {
            Kind = AccountKind.Savings,
            ParentAccountId = parent.Id,
        };
    }

    /// <summary>Define ou remove a meta da caixinha. Null remove.</summary>
    public void SetSavingsGoal(decimal? goal)
    {
        if (Kind != AccountKind.Savings)
            throw new DomainException("Apenas caixinhas têm meta.");
        if (goal is { } g && g <= 0)
            throw new DomainException("A meta deve ser maior que zero.");

        SavingsGoal = goal;
    }

    public void RenameSavingsBox(string name)
    {
        if (Kind != AccountKind.Savings)
            throw new DomainException("Apenas caixinhas têm nome.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A caixinha precisa de um nome.");

        Name = NormalizeName(name);
    }

    protected Account() { }

    public static Account Create(
       Guid userId,
       Guid householdId,
       Bank bank,
       string? name,
       Guid memberProfileId)
    {
        return new Account(userId, householdId, bank, name, memberProfileId);
    }

    /// <summary>
    /// Atualiza banco, apelido e titular. Na caixinha o nome é obrigatório, então um
    /// valor vazio preserva o atual; na conta corrente vazio remove o apelido.
    /// </summary>
    public void UpdateOwnership(
        Bank bank,
        string? name,
        Guid? memberProfileId = null)
    {
        if (!IsActive)
            throw new DomainException("Conta inativa não pode ser atualizada.");
        if (memberProfileId == Guid.Empty)
            throw new DomainException("A conta deve estar vinculada a um membro da família.");

        Bank = bank;

        var normalized = NormalizeName(name);
        if (Kind != AccountKind.Savings || normalized is not null)
            Name = normalized;

        if (memberProfileId.HasValue)
            MemberProfileId = memberProfileId.Value;
    }

    /// <summary>Trim; vazio vira null. Rejeita acima de <see cref="NameMaxLength"/>.</summary>
    private static string? NormalizeName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        if (trimmed.Length > NameMaxLength)
            throw new DomainException($"O nome deve ter no máximo {NameMaxLength} caracteres.");
        return trimmed;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("A conta já está inativa.");

        IsActive = false;
    }

    /// <summary>Transferência de titularidade da conta no mesmo lar (ex.: fusão de perfis vinculados).</summary>
    public void ReassignUserId(Guid newUserId)
    {
        if (newUserId == Guid.Empty)
            throw new DomainException("Usuário inválido.");

        UserId = newUserId;
    }

    /// <summary>Move a conta para outro lar (ex.: fusão ao aceitar convite).</summary>
    public void RelocateToHousehold(Guid newHouseholdId)
    {
        if (newHouseholdId == Guid.Empty)
            throw new DomainException("Lar inválido.");

        HouseholdId = newHouseholdId;
    }
}
