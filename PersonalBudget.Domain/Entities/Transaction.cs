public class Transaction
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? CreditCardId { get; private set; }
    public Guid? StatementId { get; private set; }
    public Guid? TransferId { get; private set; }
    public TransactionType Type { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public TransactionStatus Status { get; private set; }
    public TransactionFrequency Frequency { get; private set; }
    /// <summary>Data limite opcional para recorrência fixa (ex.: contrato até esta data).</summary>
    public DateTime? ExpirationDate { get; private set; }
    public Money Amount { get; private set; }
    public TransactionDate Date { get; private set; }
    public TransactionDescription Description { get; private set; }

    private Transaction(
        Guid userId,
        Guid accountId,
        Money amount,
        TransactionType type,
        PaymentMethod paymentMethod,
        TransactionDate date,
        TransactionDescription description,
        Guid? categoryId,
        Guid? creditCardId,
        Guid? statementId,
        Guid? transferId,
        TransactionFrequency frequency,
        DateTime? expirationDate)
    {
        if (userId == Guid.Empty)
            throw new DomainException("A transação deve pertencer a um usuário.");

        if (accountId == Guid.Empty)
            throw new DomainException("A transação deve pertencer a uma conta.");

        if (paymentMethod == PaymentMethod.CreditCard && creditCardId is null)
            throw new DomainException("Transação de cartão de crédito deve referenciar um cartão.");

        if (paymentMethod != PaymentMethod.CreditCard && creditCardId is not null)
            throw new DomainException("Apenas transações de cartão de crédito podem referenciar um cartão.");

        Id = Guid.NewGuid();
        UserId = userId;
        AccountId = accountId;
        CategoryId = categoryId;
        CreditCardId = creditCardId;
        StatementId = statementId;
        TransferId = transferId;
        Amount = amount;
        Type = type;
        PaymentMethod = paymentMethod;
        Date = date;
        Description = description;
        Frequency = frequency;
        ExpirationDate = expirationDate.HasValue ? expirationDate.Value.Date : null;
        Status = TransactionStatus.Pending;
    }

    protected Transaction() { }

    public static Transaction Create(
        Guid userId,
        Guid accountId,
        Money amount,
        TransactionType type,
        PaymentMethod paymentMethod,
        DateTime date,
        string description,
        Guid? categoryId = null,
        Guid? creditCardId = null,
        Guid? statementId = null,
        Guid? transferId = null,
        TransactionFrequency frequency = TransactionFrequency.Variable,
        DateTime? expirationDate = null)
    {
        return new Transaction(
            userId,
            accountId,
            amount,
            type,
            paymentMethod,
            new TransactionDate(date),
            new TransactionDescription(description),
            categoryId,
            creditCardId,
            statementId,
            transferId,
            frequency,
            expirationDate
        );
    }

    public void Complete()
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException("Apenas transações pendentes podem ser concluídas.");

        Status = TransactionStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainException("Transações concluídas não podem ser canceladas.");

        Status = TransactionStatus.Cancelled;
    }

    /// <summary>
    /// Altera o status da transação. Não permitido para transações de cartão de crédito.
    /// Transições: Pending→Completed, Pending→Cancelled, Cancelled→Pending, Completed→Pending.
    /// </summary>
    public void SetStatus(TransactionStatus newStatus)
    {
        if (PaymentMethod == PaymentMethod.CreditCard || CreditCardId is not null)
            throw new DomainException("Transações de cartão de crédito não podem ter o status alterado por esta operação.");

        if (Status == newStatus)
            return;

        switch (newStatus)
        {
            case TransactionStatus.Pending:
                if (Status != TransactionStatus.Cancelled && Status != TransactionStatus.Completed)
                    throw new DomainException("Apenas transações canceladas ou concluídas podem voltar para pendente.");
                Status = TransactionStatus.Pending;
                break;
            case TransactionStatus.Completed:
                if (Status != TransactionStatus.Pending)
                    throw new DomainException("Apenas transações pendentes podem ser concluídas.");
                Status = TransactionStatus.Completed;
                break;
            case TransactionStatus.Cancelled:
                if (Status == TransactionStatus.Completed)
                    throw new DomainException("Transações concluídas não podem ser canceladas.");
                Status = TransactionStatus.Cancelled;
                break;
            default:
                throw new DomainException($"O status {newStatus} não é permitido para esta operação.");
        }
    }
}
