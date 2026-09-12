public class CreditCard
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid HouseholdId { get; private set; }
    public Guid AccountId { get; private set; }

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
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public bool IsActive { get; private set; }
    /// <summary>CSS hex color, e.g. "#818cf8". Optional display hint for UI rendering.</summary>
    public string? Color { get; private set; }
    private readonly List<CreditCardStatement> _statements = new();
    public IReadOnlyCollection<CreditCardStatement> Statements => _statements.AsReadOnly();

    private CreditCard(
        Guid userId,
        Guid householdId,
        Guid accountId,
        string name,
        decimal limit,
        int closingDay,
        int dueDay,
        string? color = null,
        Guid? memberId = null)
    {
        if (limit <= 0)
            throw new DomainException("O limite do cartão de crédito deve ser maior que zero.");
        if (householdId == Guid.Empty)
            throw new DomainException("Cartão deve pertencer a um lar.");

        Id = Guid.NewGuid();
        UserId = userId;
        HouseholdId = householdId;
        AccountId = accountId;
        Name = name;
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
        IsActive = true;
        Color = color;
        MemberId = memberId;
    }

    protected CreditCard() { }

    public static CreditCard Create(
        Guid userId,
        Guid householdId,
        Guid accountId,
        string name,
        decimal limit,
        int closingDay,
        int dueDay,
        string? color = null,
        Guid? memberId = null)
        => new(userId, householdId, accountId, name, limit, closingDay, dueDay, color, memberId);

    /// <summary>
    /// Atualiza os dados do cartão. <paramref name="accountId"/> nulo mantém a
    /// conta atual - necessário para trocar a conta base quando a original foi
    /// desativada, já que AccountId é obrigatório e o cartão ficaria preso a
    /// uma conta que não existe mais na interface.
    /// </summary>
    public void Update(string name, decimal limit, int closingDay, int dueDay, string? color = null, Guid? accountId = null, Guid? memberId = null)
    {
        if (!IsActive)
            throw new DomainException("Cartão de crédito inativo não pode ser atualizado.");

        Name = name;
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
        Color = color;

        if (accountId is { } id && id != Guid.Empty)
            AccountId = id;

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

    public CreditCardStatement GetOrCreateOpenStatement(DateTime transactionDate)
    {
        var statement = _statements
            .FirstOrDefault(x =>
                x.PeriodStart <= transactionDate &&
                x.PeriodEnd >= transactionDate);

        if (statement != null)
            return statement;

        statement = CreateStatement(transactionDate);

        _statements.Add(statement);

        return statement;
    }

    private CreditCardStatement CreateStatement(DateTime date)
    {
        var year = date.Year;
        var month = date.Month;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var closingDay = Math.Clamp(ClosingDay, 1, daysInMonth);

        var periodEnd = DateTime.SpecifyKind(new DateTime(year, month, closingDay), DateTimeKind.Utc);
        var periodStart = periodEnd.AddMonths(-1).AddDays(1);

        var closingDate = periodEnd;

        var dueDate = closingDate.AddDays(DueDay);

        return new CreditCardStatement(
            Id,
            periodStart,
            periodEnd,
            closingDate,
            dueDate);
    }

    public CreditCardStatement AddExpense(DateTime date, Money amount)
    {
        var statement = GetOrCreateOpenStatement(date);

        statement.AddTransaction(amount, TransactionType.Expense);

        return statement;
    }

    public void CloseStatement(Guid statementId)
    {
        var statement = _statements.FirstOrDefault(x => x.Id == statementId);

        if (statement == null)
            throw new DomainException("Fatura não encontrada.");

        statement.Close();
    }

    public void ReopenStatement(Guid statementId)
    {
        var statement = _statements.FirstOrDefault(x => x.Id == statementId);

        if (statement == null)
            throw new DomainException("Fatura não encontrada.");

        statement.Reopen();
    }

    public void ReversePaymentStatement(Guid statementId)
    {
        var statement = _statements.FirstOrDefault(x => x.Id == statementId);

        if (statement == null)
            throw new DomainException("Fatura não encontrada.");

        statement.ReversePayment();
    }

    public decimal PayStatement(Guid statementId, Guid accountId)
    {
        var statement = _statements.FirstOrDefault(x => x.Id == statementId);

        if (statement == null)
            throw new DomainException("Fatura não encontrada.");

        statement.MarkAsPaid(accountId);

        return statement.TotalAmount.Amount;
    }
}
