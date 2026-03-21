public class CreditCardStatement
{
    public Guid Id { get; private set; }

    public Guid CreditCardId { get; private set; }

    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }

    public DateTime ClosingDate { get; private set; }
    public DateTime DueDate { get; private set; }

    /// <summary>
    /// Mês (1-12) e ano de fechamento da fatura, derivados de <see cref="ClosingDate"/>.
    /// Mantidos como colunas simples para facilitar queries e agrupamentos.
    /// </summary>
    public int ClosingMonth { get; private set; }
    public int ClosingYear { get; private set; }

    public BillStatus Status { get; private set; }

    public Money TotalAmount { get; private set; }

    protected CreditCardStatement() { }

    public CreditCardStatement(
        Guid creditCardId,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime closingDate,
        DateTime dueDate)
    {
        Id = Guid.NewGuid();
        CreditCardId = creditCardId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        ClosingDate = closingDate;
        DueDate = dueDate;
        ClosingMonth = closingDate.Month;
        ClosingYear = closingDate.Year;
        Status = BillStatus.Open;
        TotalAmount = new Money(0);
    }

    public static CreditCardStatement Open(
        Guid creditCardId,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime closingDate,
        DateTime dueDate)
    {
        return new CreditCardStatement(
            creditCardId,
            periodStart,
            periodEnd,
            closingDate,
            dueDate
        );
    }

    /// <summary>
    /// Creates a new open statement for the period that contains the given date.
    /// Period logic matches CreditCard: closingDay defines period end.
    /// </summary>
    public static CreditCardStatement CreateForDate(
        Guid creditCardId,
        DateTime date,
        int closingDay,
        int dueDay)
    {
        int year = date.Year, month = date.Month;
        int day = Math.Min(closingDay, DateTime.DaysInMonth(year, month));
        var thisMonthEnd = new DateTime(year, month, day);
        DateTime periodEnd;
        if (date.Date <= thisMonthEnd)
            periodEnd = thisMonthEnd;
        else
        {
            var next = date.AddMonths(1);
            periodEnd = new DateTime(next.Year, next.Month, Math.Min(closingDay, DateTime.DaysInMonth(next.Year, next.Month)));
        }
        var periodStart = periodEnd.AddMonths(-1).AddDays(1);
        var closingDate = periodEnd;
        var dueDate = closingDate.AddDays(dueDay);
        return Open(creditCardId, periodStart, periodEnd, closingDate, dueDate);
    }

    /// <summary>
    /// Atualiza o total da fatura conforme o tipo: despesa aumenta o que se deve;
    /// receita (reembolso, estorno) reduz o saldo da fatura.
    /// </summary>
    public void AddTransaction(Money amount, TransactionType transactionType)
    {
        if (Status != BillStatus.Open)
            throw new DomainException("Não é possível adicionar transação a uma fatura fechada.");

        if (transactionType == TransactionType.Expense)
        {
            TotalAmount = TotalAmount.Add(amount);
            return;
        }

        if (transactionType == TransactionType.Income)
        {
            var newTotal = TotalAmount.Amount - amount.Amount;
            if (newTotal < 0)
                newTotal = 0;
            TotalAmount = new Money(newTotal);
            return;
        }

        throw new DomainException($"Tipo de transação não suportado na fatura: {transactionType}.");
    }

    public void Close()
    {
        if (Status != BillStatus.Open)
            throw new DomainException("A fatura já está fechada.");

        Status = BillStatus.Closed;
    }

    public void MarkAsPaid()
    {
        if (Status == BillStatus.Paid)
            throw new DomainException("A fatura já foi paga.");

        if (Status == BillStatus.Open)
            Close();

        Status = BillStatus.Paid;
    }
}