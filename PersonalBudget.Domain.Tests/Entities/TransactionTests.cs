using FluentAssertions;

public class TransactionTests
{
    // =========================
    // HELPERS
    // =========================

    private Transaction CreateValidTransaction(
        PaymentMethod paymentMethod = PaymentMethod.Pix,
        Guid? creditCardId = null)
    {
        return new Transaction(
            accountId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            amount: new Money(100),
            type: TransactionType.Expense,
            paymentMethod: paymentMethod,
            occurredAt: DateTime.Today,
            creditCardId: creditCardId
        );
    }

    // =========================
    // CONSTRUCTION TESTS
    // =========================

    [Fact]
    public void Transaction_Should_Be_Created_As_Pending()
    {
        var transaction = CreateValidTransaction();

        transaction.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public void Transaction_Should_Have_Valid_Identity()
    {
        var transaction = CreateValidTransaction();

        transaction.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Transaction_Without_Account_Should_Fail()
    {
        Action act = () =>
            new Transaction(
                accountId: Guid.Empty,
                categoryId: Guid.NewGuid(),
                amount: new Money(100),
                type: TransactionType.Expense,
                paymentMethod: PaymentMethod.Pix,
                occurredAt: DateTime.Today
            );

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Transaction_Without_Category_Should_Fail()
    {
        Action act = () =>
            new Transaction(
                accountId: Guid.NewGuid(),
                categoryId: Guid.Empty,
                amount: new Money(100),
                type: TransactionType.Expense,
                paymentMethod: PaymentMethod.Pix,
                occurredAt: DateTime.Today
            );

        act.Should().Throw<DomainException>();
    }
    
    // =========================
    // PAYMENT METHOD RULES
    // =========================

    [Fact]
    public void CreditCard_Transaction_Must_Have_CreditCardId()
    {
        Action act = () =>
            CreateValidTransaction(
                paymentMethod: PaymentMethod.CreditCard,
                creditCardId: null
            );

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Non_CreditCard_Transaction_Cannot_Have_CreditCardId()
    {
        Action act = () =>
            CreateValidTransaction(
                paymentMethod: PaymentMethod.Pix,
                creditCardId: Guid.NewGuid()
            );

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreditCard_Transaction_With_CreditCardId_Should_Be_Created()
    {
        var transaction = CreateValidTransaction(
            paymentMethod: PaymentMethod.CreditCard,
            creditCardId: Guid.NewGuid()
        );

        transaction.PaymentMethod.Should().Be(PaymentMethod.CreditCard);
        transaction.CreditCardId.Should().NotBeNull();
    }

    // =========================
    // STATE TRANSITIONS
    // =========================

    [Fact]
    public void Transaction_Can_Be_Marked_As_Completed()
    {
        var transaction = CreateValidTransaction();

        transaction.MarkAsCompleted();

        transaction.Status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public void Transaction_Cannot_Be_Completed_Twice()
    {
        var transaction = CreateValidTransaction();
        transaction.MarkAsCompleted();

        Action act = () => transaction.MarkAsCompleted();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Completed_Transaction_Is_Completed()
    {
        var transaction = CreateValidTransaction();

        transaction.MarkAsCompleted();

        transaction.IsCompleted().Should().BeTrue();
    }

    // =========================
    // SIMULATION RULES
    // =========================

    [Fact]
    public void Transaction_Can_Be_Marked_As_Simulated()
    {
        var transaction = CreateValidTransaction();

        transaction.MarkAsSimulated();

        transaction.Status.Should().Be(TransactionStatus.Simulated);
    }

    [Fact]
    public void Completed_Transaction_Cannot_Be_Simulated()
    {
        var transaction = CreateValidTransaction();
        transaction.MarkAsCompleted();

        Action act = () => transaction.MarkAsSimulated();

        act.Should().Throw<DomainException>();
    }

    // =========================
    // IMMUTABILITY TESTS
    // =========================

    [Fact]
    public void Transaction_Core_Properties_Should_Not_Change()
    {
        var transaction = CreateValidTransaction();
        var id = transaction.Id;
        var accountId = transaction.AccountId;
        var categoryId = transaction.CategoryId;
        var amount = transaction.Amount;

        transaction.MarkAsCompleted();

        transaction.Id.Should().Be(id);
        transaction.AccountId.Should().Be(accountId);
        transaction.CategoryId.Should().Be(categoryId);
        transaction.Amount.Should().Be(amount);
    }
}