public class InstallmentPlan
{
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
        InstallmentAmount = new Money(totalAmount.Amount / totalInstallments);
        StartDate = startDate;
        IsCancelled = false;
    }

    protected InstallmentPlan() { }

    public void Cancel()
    {
        IsCancelled = true;
    }

    public IEnumerable<Transaction> GenerateTransactions()
    {
        return null; // To be implemented
    }

}