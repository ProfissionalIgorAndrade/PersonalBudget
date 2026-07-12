public class Transaction
{
    public Guid Id { get; private set; }
    /// <summary>Usuário que registrou o lançamento.</summary>
    public Guid UserId { get; private set; }
    /// <summary>Lar ao qual a transação pertence (visão unificada).</summary>
    public Guid HouseholdId { get; private set; }
    /// <summary>Correspondente para resumo por pessoa (Igor, Andreza, Família, etc.).</summary>
    public Guid AttributionProfileId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? CreditCardId { get; private set; }
    public Guid? StatementId { get; private set; }
    public Guid? TransferId { get; private set; }
    /// <summary>Agrupa transações da mesma série recorrente ou parcelamento. Null para lançamentos avulsos.</summary>
    public Guid? RecurrenceId { get; private set; }
    public TransactionType Type { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public TransactionStatus Status { get; private set; }
    public TransactionFrequency Frequency { get; private set; }
    /// <summary>Data limite opcional para recorrência fixa (ex.: contrato até esta data).</summary>
    public DateTime? ExpirationDate { get; private set; }
    /// <summary>Data de vencimento opcional do lançamento (quando informada pelo cliente).</summary>
    public DateTime? DueDate { get; private set; }
    public Money Amount { get; private set; }
    public TransactionDate Date { get; private set; }
    public TransactionDescription Description { get; private set; }

    private Transaction(
        Guid userId,
        Guid householdId,
        Guid attributionProfileId,
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
        DateTime? expirationDate,
        DateTime? dueDate,
        TransactionStatus initialStatus)
    {
        if (userId == Guid.Empty)
            throw new DomainException("A transação deve pertencer a um usuário.");
        if (householdId == Guid.Empty)
            throw new DomainException("A transação deve pertencer a um lar.");
        if (attributionProfileId == Guid.Empty)
            throw new DomainException("Correspondente (perfil) da transação é obrigatório.");

        if (accountId == Guid.Empty)
            throw new DomainException("A transação deve pertencer a uma conta.");

        if (paymentMethod == PaymentMethod.CreditCard && creditCardId is null)
            throw new DomainException("Transação de cartão de crédito deve referenciar um cartão.");

        if (paymentMethod != PaymentMethod.CreditCard && creditCardId is not null)
            throw new DomainException("Apenas transações de cartão de crédito podem referenciar um cartão.");

        Id = Guid.NewGuid();
        UserId = userId;
        HouseholdId = householdId;
        AttributionProfileId = attributionProfileId;
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
        ExpirationDate = expirationDate.HasValue ? DateTime.SpecifyKind(expirationDate.Value.Date, DateTimeKind.Utc) : null;
        DueDate = dueDate.HasValue ? DateTime.SpecifyKind(dueDate.Value.Date, DateTimeKind.Utc) : null;
        Status = initialStatus;
    }

    protected Transaction() { }

    public static Transaction Create(
        Guid userId,
        Guid householdId,
        Guid attributionProfileId,
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
        DateTime? expirationDate = null,
        DateTime? dueDate = null,
        TransactionStatus initialStatus = TransactionStatus.Pending)
    {
        return new Transaction(
            userId,
            householdId,
            attributionProfileId,
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
            expirationDate,
            dueDate,
            initialStatus
        );
    }

    /// <summary>Atualiza dados editáveis. Não permitido para transações concluídas.</summary>
    public void UpdateDetails(
        Money newAmount,
        TransactionDate newDate,
        TransactionDescription newDescription,
        Guid? categoryId,
        DateTime? expirationDate,
        DateTime? dueDate)
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainException("Transações concluídas não podem ser editadas.");

        Amount = newAmount;
        Date = newDate;
        Description = newDescription;
        CategoryId = categoryId;
        ExpirationDate = expirationDate.HasValue ? DateTime.SpecifyKind(expirationDate.Value.Date, DateTimeKind.Utc) : null;
        DueDate = dueDate.HasValue ? DateTime.SpecifyKind(dueDate.Value.Date, DateTimeKind.Utc) : null;
    }

    /// <summary>Altera o correspondente (perfil). Não permitido para transações concluídas.</summary>
    public void UpdateAttributionProfileId(Guid attributionProfileId)
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainException("Transações concluídas não podem ser editadas.");

        if (attributionProfileId == Guid.Empty)
            throw new DomainException("Correspondente inválido.");

        AttributionProfileId = attributionProfileId;
    }

    /// <summary>Usado ao editar data da transação de cartão para outro período (fatura aberta).</summary>
    public void ChangeCreditCardStatement(Guid statementId)
    {
        if (PaymentMethod != PaymentMethod.CreditCard || CreditCardId is null)
            throw new DomainException("Apenas transações de cartão de crédito podem ter a fatura alterada.");

        if (statementId == Guid.Empty)
            throw new DomainException("Fatura inválida.");

        StatementId = statementId;
    }

    public void Complete()
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException("Apenas transações pendentes podem ser concluídas.");

        Status = TransactionStatus.Completed;
    }

    /// <summary>
    /// Reverte uma transação concluída para pendente ao estornar o pagamento de uma fatura.
    /// Operação restrita ao domínio de fatura — não exposta via SetStatus para evitar uso indevido.
    /// </summary>
    public void RevertToPending()
    {
        if (Status != TransactionStatus.Completed)
            throw new DomainException("Apenas transações concluídas podem ser revertidas para pendente.");

        Status = TransactionStatus.Pending;
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

    /// <summary>Vincula esta transação a um grupo de recorrência. Chamado uma vez após a criação em lote.</summary>
    public void AssignRecurrenceId(Guid recurrenceId)
    {
        if (recurrenceId == Guid.Empty)
            throw new DomainException("RecurrenceId inválido.");

        RecurrenceId = recurrenceId;
    }

    /// <summary>Reatribui correspondente ao fundir/excluir perfil no mesmo lar (inclui lançamentos concluídos).</summary>
    public void ReassignAttributionProfileForMerge(Guid newAttributionProfileId)
    {
        if (newAttributionProfileId == Guid.Empty)
            throw new DomainException("Correspondente inválido.");

        AttributionProfileId = newAttributionProfileId;
    }

    /// <summary>
    /// Atualiza lar, correspondente e categoria ao migrar dados para o lar do convite.
    /// Permite alterar lançamentos concluídos (regra normal de edição não se aplica).
    /// </summary>
    public void ApplyInviteAcceptanceMigration(
        Guid newHouseholdId,
        Guid newAttributionProfileId,
        Guid? newCategoryId)
    {
        if (newHouseholdId == Guid.Empty)
            throw new DomainException("Lar inválido.");
        if (newAttributionProfileId == Guid.Empty)
            throw new DomainException("Correspondente inválido.");

        HouseholdId = newHouseholdId;
        AttributionProfileId = newAttributionProfileId;
        CategoryId = newCategoryId;
    }
}
