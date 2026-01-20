using FluentAssertions;

public class TransactionTests
{
    private Transaction CreateValidTransaction()
    {
        return new Transaction(
            accountId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            amount: new Money(100),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Pix,
            occurredAt: DateTime.Today
        );
    }

    [Fact]
    public void Should_create_transaction_as_pending()
    {
        var transaction = new Transaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(100),
            TransactionType.Expense,
            PaymentMethod.DebitCard,
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
                PaymentMethod.DebitCard,
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
                PaymentMethod.DebitCard,
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
            PaymentMethod.DebitCard,
            DateTime.UtcNow
        );

        transaction.MarkAsCompleted();

        Assert.Throws<DomainException>(() => transaction.MarkAsCompleted());
    }

    [Fact]
    public void Transaction_Should_Start_As_Pending()
    {
        var transaction = CreateValidTransaction();

        transaction.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public void Transaction_With_CreditCard_Must_Have_CreditCardId()
    {
        Action act = () =>
            new Transaction(
                accountId: Guid.NewGuid(),
                categoryId: Guid.NewGuid(),
                amount: new Money(100),
                type: TransactionType.Expense,
                paymentMethod: PaymentMethod.CreditCard,
                occurredAt: DateTime.Today,
                creditCardId: null
            );

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Completed_Transaction_Cannot_Be_Completed_Again()
    {
        var transaction = CreateValidTransaction();

        transaction.MarkAsCompleted();

        Action act = () => transaction.MarkAsCompleted();

        act.Should().Throw<DomainException>();
    }
}
