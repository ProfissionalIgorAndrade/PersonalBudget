public class TransactionApplierTests
{
    [Fact]
    public void Should_not_apply_pending_transaction()
    {
        var account = new Account("Main", new Money(100));

        var transaction = new Transaction(
            account.Id,
            Guid.NewGuid(),
            new Money(50),
            TransactionType.Expense,
            TransactionStatus.Pending,
            DateTime.UtcNow
        );

        TransactionApplier.Apply(account, transaction);

        Assert.Equal(100, account.Balance.Amount);
    }

    [Fact]
    public void Should_apply_completed_income_transaction()
    {
        var account = new Account("Main", new Money(100));

        var transaction = new Transaction(
            account.Id,
            Guid.NewGuid(),
            new Money(50),
            TransactionType.Income,
            TransactionStatus.Pending,
            DateTime.UtcNow
        );

        transaction.MarkAsCompleted();

        TransactionApplier.Apply(account, transaction);

        Assert.Equal(150, account.Balance.Amount);
    }

    [Fact]
    public void Should_apply_completed_expense_transaction()
    {
        var account = new Account("Main", new Money(100));

        var transaction = new Transaction(
            account.Id,
            Guid.NewGuid(),
            new Money(30),
            TransactionType.Expense,
            TransactionStatus.Pending,
            DateTime.UtcNow
        );

        transaction.MarkAsCompleted();

        TransactionApplier.Apply(account, transaction);

        Assert.Equal(70, account.Balance.Amount);
    }
}
