using FluentAssertions;

public class InstallmentPlanTests
{
    private InstallmentPlan CreateValidInstallmentPlan(
            DateTime? startDate = null)
    {
        return new InstallmentPlan(
            accountId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            creditCardId: Guid.NewGuid(),
            paymentMethod: PaymentMethod.CreditCard,
            description: "Compra Casa Bahia TV",
            totalAmount: new Money(1200),
            totalInstallments: 12,
            startDate: startDate ?? new DateTime(2025, 1, 10)
        );
    }

    [Fact]
    public void InstallmentPlan_Should_Generate_Correct_Number_Of_Transactions()
    {
        var plan = CreateValidInstallmentPlan();

        var transactions = plan.GenerateTransactions().ToList();

        transactions.Should().HaveCount(plan.TotalInstallments);
    }

    [Fact]
    public void InstallmentPlan_Should_Not_Generate_Transactions_When_Cancelled()
    {
        var plan = CreateValidInstallmentPlan();
        plan.Cancel();

        Action act = () => plan.GenerateTransactions().ToList();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void InstallmentPlan_Transactions_Should_Have_Correct_Dates()
    {
        var startDate = new DateTime(2025, 1, 10);
        var plan = CreateValidInstallmentPlan(startDate);

        var transactions = plan.GenerateTransactions().ToList();

        transactions[0].OccurredAt.Should().Be(startDate);
        transactions[1].OccurredAt.Should().Be(startDate.AddMonths(1));
    }

}
