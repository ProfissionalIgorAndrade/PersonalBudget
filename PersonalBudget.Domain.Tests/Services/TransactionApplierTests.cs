public class TransactionApplierTests
{
    private static Account CreateValidAccount(
        decimal initialBalance = 100)
    {
        return new Account(
            userId: Guid.NewGuid(),
            bank: Bank.Nubank,
            agency: new BankAgency("0001"),
            number: new BankAccountNumber("123456-7"),
            initialBalance: new Money(initialBalance)
        );
    }
    
    [Fact]
    public void Should_not_apply_pending_transaction()
    {
        var account = CreateValidAccount();

        var transaction = new Transaction(
            account.Id,
            Guid.NewGuid(),
            new Money(50),
            TransactionType.Expense,
            PaymentMethod.DebitCard,
            DateTime.UtcNow
        );

        TransactionApplier.Apply(account, transaction);

        Assert.Equal(100, account.Balance.Amount);
    }

    [Fact]
    public void Should_apply_completed_income_transaction()
    {
        var account = CreateValidAccount(100);

        var transaction = new Transaction(
            account.Id,
            Guid.NewGuid(),
            new Money(50),
            TransactionType.Income,
            PaymentMethod.DebitCard,
            DateTime.UtcNow
        );

        transaction.MarkAsCompleted();

        TransactionApplier.Apply(account, transaction);

        Assert.Equal(150, account.Balance.Amount);
    }

    [Fact]
    public void Should_apply_completed_expense_transaction()
    {
        var account = CreateValidAccount(100);

        var transaction = new Transaction(
            account.Id,
            Guid.NewGuid(),
            new Money(30),
            TransactionType.Expense,
            PaymentMethod.DebitCard,
            DateTime.UtcNow
        );

        transaction.MarkAsCompleted();

        TransactionApplier.Apply(account, transaction);

        Assert.Equal(70, account.Balance.Amount);
    }
}
