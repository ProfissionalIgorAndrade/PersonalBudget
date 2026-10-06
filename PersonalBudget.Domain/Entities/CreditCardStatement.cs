public class CreditCardStatement
{
    public Guid Id { get; private set; }

    public Guid CreditCardId { get; private set; }

    /// <summary>
    /// Mês (1-12) e ano da fatura, que correspondem ao mês do vencimento.
    /// A fatura é identificada por (cartão, mês, ano), com índice único no banco.
    /// O vencimento em si não é guardado: é o dia de vencimento do cartão neste mês.
    /// </summary>
    public int StatementMonth { get; private set; }
    public int StatementYear { get; private set; }

    public Money TotalAmount { get; private set; }

    protected CreditCardStatement() { }

    private CreditCardStatement(Guid creditCardId, int month, int year)
    {
        if (creditCardId == Guid.Empty)
            throw new DomainException("A fatura deve pertencer a um cartão.");
        if (month < 1 || month > 12)
            throw new DomainException("O mês da fatura deve estar entre 1 e 12.");
        if (year < 1 || year > 9999)
            throw new DomainException("O ano da fatura é inválido.");

        Id = Guid.NewGuid();
        CreditCardId = creditCardId;
        StatementMonth = month;
        StatementYear = year;
        TotalAmount = new Money(0);
    }

    /// <summary>Cria a fatura do cartão para o mês/ano informado, com total zerado.</summary>
    public static CreditCardStatement Create(Guid creditCardId, int month, int year)
        => new(creditCardId, month, year);

    /// <summary>
    /// Vencimento da fatura: o dia de vencimento do cartão no mês da fatura,
    /// limitado ao último dia do mês (dia 31 em fevereiro vence dia 28 ou 29).
    /// </summary>
    public DateTime DueDateFor(int dueDay)
    {
        var day = Math.Clamp(dueDay, 1, DateTime.DaysInMonth(StatementYear, StatementMonth));
        return DateTime.SpecifyKind(new DateTime(StatementYear, StatementMonth, day), DateTimeKind.Utc);
    }

    /// <summary>
    /// Atualiza o total da fatura conforme o tipo: despesa aumenta o que se deve;
    /// receita (reembolso, estorno) reduz o saldo da fatura.
    /// </summary>
    public void AddTransaction(Money amount, TransactionType transactionType)
    {
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

    /// <summary>Desfaz o efeito de um lançamento no total (edição ou remoção).</summary>
    public void RemoveTransactionContribution(Money amount, TransactionType transactionType)
    {
        if (transactionType == TransactionType.Expense)
        {
            var newTotal = TotalAmount.Amount - amount.Amount;
            if (newTotal < 0)
                newTotal = 0;
            TotalAmount = new Money(newTotal);
            return;
        }

        if (transactionType == TransactionType.Income)
        {
            TotalAmount = TotalAmount.Add(amount);
            return;
        }

        throw new DomainException($"Tipo de transação não suportado na fatura: {transactionType}.");
    }

    public void ReplaceTransactionContribution(Money oldAmount, Money newAmount, TransactionType transactionType)
    {
        RemoveTransactionContribution(oldAmount, transactionType);
        AddTransaction(newAmount, transactionType);
    }

}
