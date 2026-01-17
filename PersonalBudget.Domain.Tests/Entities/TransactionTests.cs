public class TransactionTests
{
    [Fact]
    public void Should_create_transaction_as_pending()
    {
        var transaction = new Transaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(100),
            TransactionType.Expense,
            TransactionStatus.Pending,
            DateTime.UtcNow
        );

        Assert.Equal(TransactionStatus.Pending, transaction.Status);
    }

    [Fact]
    public void Should_not_allow_empty_account()
    {
        Assert.Throws<DomainException>(() =>
            new Transaction(
                Guid.Empty,
                Guid.NewGuid(),
                new Money(100),
                TransactionType.Expense,
                TransactionStatus.Pending,
                DateTime.UtcNow
            ));
    }

    [Fact]
    public void Should_not_allow_empty_category()
    {
        Assert.Throws<DomainException>(() =>
            new Transaction(
                Guid.NewGuid(),
                Guid.Empty,
                new Money(100),
                TransactionType.Expense,
                TransactionStatus.Pending,
                DateTime.UtcNow
            ));
    }

    [Fact]
    public void Should_not_allow_completing_transaction_twice()
    {
        var transaction = new Transaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(100),
            TransactionType.Income,
            TransactionStatus.Pending,
            DateTime.UtcNow
        );

        transaction.MarkAsCompleted();

        Assert.Throws<DomainException>(() => transaction.MarkAsCompleted());
    }
}
