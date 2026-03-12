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
        Guid? transferId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Transaction must belong to a user.");

        if (accountId == Guid.Empty)
            throw new DomainException("Transaction must belong to an account.");

        if (paymentMethod == PaymentMethod.CreditCard && creditCardId is null)
            throw new DomainException("Credit card transaction must reference a credit card.");

        if (paymentMethod != PaymentMethod.CreditCard && creditCardId is not null)
            throw new DomainException("Only credit card transactions can reference a credit card.");

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
        Guid? transferId = null)
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
            transferId
        );
    }

    public void Complete()
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException("Only pending transactions can be completed.");

        Status = TransactionStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainException("Completed transactions cannot be cancelled.");

        Status = TransactionStatus.Cancelled;
    }

    /// <summary>
    /// Altera o status da transação. Não permitido para transações de cartão de crédito.
    /// Transições: Pending→Completed, Pending→Cancelled, Cancelled→Pending, Completed→Pending.
    /// </summary>
    public void SetStatus(TransactionStatus newStatus)
    {
        if (PaymentMethod == PaymentMethod.CreditCard || CreditCardId is not null)
            throw new DomainException("Credit card transactions cannot have their status changed by this operation.");

        if (Status == newStatus)
            return;

        switch (newStatus)
        {
            case TransactionStatus.Pending:
                if (Status != TransactionStatus.Cancelled && Status != TransactionStatus.Completed)
                    throw new DomainException("Only cancelled or completed transactions can be set back to pending.");
                Status = TransactionStatus.Pending;
                break;
            case TransactionStatus.Completed:
                if (Status != TransactionStatus.Pending)
                    throw new DomainException("Only pending transactions can be completed.");
                Status = TransactionStatus.Completed;
                break;
            case TransactionStatus.Cancelled:
                if (Status == TransactionStatus.Completed)
                    throw new DomainException("Completed transactions cannot be cancelled.");
                Status = TransactionStatus.Cancelled;
                break;
            default:
                throw new DomainException($"Status {newStatus} is not allowed for this operation.");
        }
    }
}
