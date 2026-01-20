public class InstallmentPlan
{
    public InstallmentPlan(
        Guid accountId,
        Guid categoryId,
        Guid creditCardId,
        PaymentMethod paymentMethod,
        string description,
        Money totalAmount,
        int totalInstallments,
        DateTime startDate)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("Installment plan must have an account.");

        if (categoryId == Guid.Empty)
            throw new DomainException("Installment plan must have a category.");

        if (creditCardId == Guid.Empty)
            throw new DomainException("Installment plan must have a credit card.");

        if (paymentMethod != PaymentMethod.CreditCard)
            throw new DomainException("Installment plan payment method must be credit card.");

        if (totalInstallments <= 1)
            throw new DomainException("Installment plan must have at least one installment.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Installment plan must have a description.");

        Id = Guid.NewGuid();
        AccountId = accountId;
        CategoryId = categoryId;
        CreditCardId = creditCardId;
        PaymentMethod = paymentMethod;

        Description = description;
        TotalInstallments = totalInstallments;
        TotalAmount = totalAmount;

        InstallmentAmount = new Money(
            totalAmount.Amount / totalInstallments);

        StartDate = startDate;
        IsCancelled = false;
    }

    public Guid Id { get; }
    public Guid AccountId { get; }
    public Guid CategoryId { get; }
    public Guid CreditCardId { get; }
    public PaymentMethod PaymentMethod { get; }
    public string Description { get; }
    public Money TotalAmount { get; }
    public int TotalInstallments { get; }
    public Money InstallmentAmount { get; }
    public DateTime StartDate { get; }
    public bool IsCancelled { get; private set; }

    public void Cancel()
    {
        IsCancelled = true;
    }

    public IEnumerable<Transaction> GenerateTransactions()
    {
        if (IsCancelled)
            throw new DomainException("Cancelled installment plan cannot generate transactions.");

        var transactions = new List<Transaction>();

        for (int i = 0; i < TotalInstallments; i++)
        {
            var date = StartDate.AddMonths(i);

            var transaction = new Transaction(
                accountId: AccountId,
                categoryId: CategoryId,
                amount: InstallmentAmount,
                type: TransactionType.Expense,
                paymentMethod: PaymentMethod,
                occurredAt: date,
                creditCardId: CreditCardId
            );

            transactions.Add(transaction);
        }

        return transactions;
    }

}