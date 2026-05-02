public class CreditCard
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid HouseholdId { get; private set; }
    public Guid AccountId { get; private set; }
    public string Name { get; private set; }
    public decimal Limit { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public bool IsActive { get; private set; }
    private readonly List<CreditCardStatement> _statements = new();
    public IReadOnlyCollection<CreditCardStatement> Statements => _statements.AsReadOnly();

    private CreditCard(
        Guid userId,
        Guid householdId,
        Guid accountId,
        string name,
        decimal limit,
        int closingDay,
        int dueDay)
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
    }

    protected CreditCard() { }

    public static CreditCard Create(
        Guid userId,
        Guid householdId,
        Guid accountId,
        string name,
        decimal limit,
        int closingDay,
        int dueDay)
        => new(userId, householdId, accountId, name, limit, closingDay, dueDay);

    public void Update(string name, decimal limit, int closingDay, int dueDay)
    {
        if (!IsActive)
            throw new DomainException("Cartão de crédito inativo não pode ser atualizado.");

        Name = name;
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
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

        var periodEnd = new DateTime(year, month, closingDay);
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

    public decimal PayStatement(Guid statementId)
    {
        var statement = _statements.FirstOrDefault(x => x.Id == statementId);

        if (statement == null)
            throw new DomainException("Fatura não encontrada.");

        statement.MarkAsPaid();

        return statement.TotalAmount.Amount;
    }
}
