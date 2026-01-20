using FluentAssertions;

public class DomainExceptionTests
{
    [Fact]
    public void Transaction_Without_CreditCardId_Should_Fail()
    {
        Action act = () =>
            new Transaction(
                accountId: Guid.NewGuid(),
                categoryId: Guid.NewGuid(),
                amount: new Money(50),
                type: TransactionType.Expense,
                paymentMethod: PaymentMethod.CreditCard,
                occurredAt: DateTime.Today,
                creditCardId: null
            );

        act.Should().Throw<DomainException>();
    }
}
