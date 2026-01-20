using FluentAssertions;

public class InstallmentPlanTests
{
    // =========================
    // HELPERS
    // =========================

    private InstallmentPlan CreateValidInstallmentPlan(
        int totalInstallments = 12,
        DateTime? startDate = null)
    {
        return new InstallmentPlan(
            accountId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            creditCardId: Guid.NewGuid(),
            paymentMethod: PaymentMethod.CreditCard,
            description: "Compra Casa Bahia TV",
            totalAmount: new Money(1200),
            totalInstallments: totalInstallments,
            startDate: startDate ?? new DateTime(2025, 1, 10)
        );
    }

    // =========================
    // CONSTRUCTION INVARIANTS
    // =========================

    [Fact]
    public void InstallmentPlan_Should_Have_Valid_Identity()
    {
        var plan = CreateValidInstallmentPlan();

        plan.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void InstallmentPlan_Without_Account_Should_Fail()
    {
        Action act = () =>
            new InstallmentPlan(
                accountId: Guid.Empty,
                categoryId: Guid.NewGuid(),
                creditCardId: Guid.NewGuid(),
                paymentMethod: PaymentMethod.CreditCard,
                description: "Compra",
                totalAmount: new Money(1000),
                totalInstallments: 10,
                startDate: DateTime.Today
            );

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void InstallmentPlan_Without_Category_Should_Fail()
    {
        Action act = () =>
            new InstallmentPlan(
                accountId: Guid.NewGuid(),
                categoryId: Guid.Empty,
                creditCardId: Guid.NewGuid(),
                paymentMethod: PaymentMethod.CreditCard,
                description: "Compra",
                totalAmount: new Money(1000),
                totalInstallments: 10,
                startDate: DateTime.Today
            );

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void InstallmentPlan_Without_Description_Should_Fail()
    {
        Action act = () =>
            new InstallmentPlan(
                accountId: Guid.NewGuid(),
                categoryId: Guid.NewGuid(),
                creditCardId: Guid.NewGuid(),
                paymentMethod: PaymentMethod.CreditCard,
                description: "",
                totalAmount: new Money(1000),
                totalInstallments: 10,
                startDate: DateTime.Today
            );

        act.Should().Throw<DomainException>();
    }

    // =========================
    // PAYMENT METHOD RULES
    // =========================

    [Fact]
    public void InstallmentPlan_Must_Use_CreditCard()
    {
        Action act = () =>
            new InstallmentPlan(
                accountId: Guid.NewGuid(),
                categoryId: Guid.NewGuid(),
                creditCardId: Guid.NewGuid(),
                paymentMethod: PaymentMethod.Pix,
                description: "Compra",
                totalAmount: new Money(1000),
                totalInstallments: 10,
                startDate: DateTime.Today
            );

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void InstallmentPlan_Without_CreditCardId_Should_Fail()
    {
        Action act = () =>
            new InstallmentPlan(
                accountId: Guid.NewGuid(),
                categoryId: Guid.NewGuid(),
                creditCardId: Guid.Empty,
                paymentMethod: PaymentMethod.CreditCard,
                description: "Compra",
                totalAmount: new Money(1000),
                totalInstallments: 10,
                startDate: DateTime.Today
            );

        act.Should().Throw<DomainException>();
    }

    // =========================
    // INSTALLMENT RULES
    // =========================

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void InstallmentPlan_With_Invalid_Number_Of_Installments_Should_Fail(int installments)
    {
        Action act = () =>
            CreateValidInstallmentPlan(totalInstallments: installments);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void InstallmentPlan_Should_Calculate_Installment_Amount()
    {
        var plan = CreateValidInstallmentPlan(totalInstallments: 12);

        plan.InstallmentAmount.Should().Be(new Money(100));
    }

    // =========================
    // GENERATE TRANSACTIONS
    // =========================

    [Fact]
    public void InstallmentPlan_Should_Generate_Correct_Number_Of_Transactions()
    {
        var plan = CreateValidInstallmentPlan(totalInstallments: 6);

        var transactions = plan.GenerateTransactions().ToList();

        transactions.Should().HaveCount(6);
    }

    [Fact]
    public void InstallmentPlan_Should_Generate_Transactions_With_Correct_Dates()
    {
        var startDate = new DateTime(2025, 1, 10);
        var plan = CreateValidInstallmentPlan(startDate: startDate);

        var transactions = plan.GenerateTransactions().ToList();

        transactions[0].OccurredAt.Should().Be(startDate);
        transactions[1].OccurredAt.Should().Be(startDate.AddMonths(1));
        transactions[2].OccurredAt.Should().Be(startDate.AddMonths(2));
    }

    [Fact]
    public void InstallmentPlan_Transactions_Should_Have_Pending_Status()
    {
        var plan = CreateValidInstallmentPlan();

        var transactions = plan.GenerateTransactions().ToList();

        transactions.Should().OnlyContain(t => t.Status == TransactionStatus.Pending);
    }

    [Fact]
    public void InstallmentPlan_Transactions_Should_Reference_InstallmentPlan()
    {
        var plan = CreateValidInstallmentPlan();

        var transactions = plan.GenerateTransactions().ToList();

        transactions.Should().OnlyContain(t => t.InstallmentPlanId == plan.Id);
    }

    [Fact]
    public void InstallmentPlan_Transactions_Should_Have_CreditCard_PaymentMethod()
    {
        var plan = CreateValidInstallmentPlan();

        var transactions = plan.GenerateTransactions().ToList();

        transactions.Should().OnlyContain(t => t.PaymentMethod == PaymentMethod.CreditCard);
    }

    [Fact]
    public void InstallmentPlan_Transactions_Should_Have_Correct_Descriptions()
    {
        var plan = CreateValidInstallmentPlan(totalInstallments: 3);

        var transactions = plan.GenerateTransactions().ToList();

        transactions[0].Description.Should().Contain("(1/3)");
        transactions[1].Description.Should().Contain("(2/3)");
        transactions[2].Description.Should().Contain("(3/3)");
    }

    // =========================
    // CANCEL RULES
    // =========================

    [Fact]
    public void InstallmentPlan_Can_Be_Cancelled()
    {
        var plan = CreateValidInstallmentPlan();

        plan.Cancel();

        plan.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void Cancelled_InstallmentPlan_Cannot_Generate_Transactions()
    {
        var plan = CreateValidInstallmentPlan();
        plan.Cancel();

        Action act = () => plan.GenerateTransactions().ToList();

        act.Should().Throw<DomainException>();
    }

    // =========================
    // IMMUTABILITY TESTS
    // =========================

    [Fact]
    public void Generating_Transactions_Should_Not_Change_InstallmentPlan_State()
    {
        var plan = CreateValidInstallmentPlan();
        var id = plan.Id;
        var total = plan.TotalAmount;

        plan.GenerateTransactions().ToList();

        plan.Id.Should().Be(id);
        plan.TotalAmount.Should().Be(total);
        plan.IsCancelled.Should().BeFalse();
    }
}
