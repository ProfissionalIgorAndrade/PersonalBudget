using FluentAssertions;

public class CreditCardTests
{
    private CreditCard CreateValidCreditCard(
    int closingDay = 10,
    int dueDay = 15)
    {
        return new CreditCard(
            accountId: Guid.NewGuid(),
            name: "Nubank",
            lastFourDigits: "1234",
            brand: "Mastercard",
            creditLimit: new Money(5000),
            closingDay: closingDay,
            dueDay: dueDay
        );
    }

    [Fact]
    public void CreditCard_Should_Be_Created_As_Active()
    {
        var card = CreateValidCreditCard();

        card.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreditCard_Can_Be_Deactivated()
    {
        var card = CreateValidCreditCard();

        card.Deactivate();

        card.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CreditCard_Without_Account_Should_Fail()
    {
        Action act = () =>
            new CreditCard(
                Guid.Empty,
                "Nubank",
                "1234",
                "Mastercard",
                new Money(5000),
                10,
                15
            );

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreditCard_Without_Name_Should_Fail()
    {
        Action act = () =>
            new CreditCard(
                Guid.NewGuid(),
                "",
                "1234",
                "Mastercard",
                new Money(5000),
                10,
                15
            );

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("123")]
    [InlineData("12345")]
    public void CreditCard_With_Invalid_LastFourDigits_Should_Fail(string digits)
    {
        Action act = () =>
            new CreditCard(
                Guid.NewGuid(),
                "Nubank",
                digits,
                "Mastercard",
                new Money(5000),
                10,
                15
            );

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(31)]
    public void CreditCard_With_Invalid_ClosingDay_Should_Fail(int day)
    {
        Action act = () =>
            new CreditCard(
                Guid.NewGuid(),
                "Nubank",
                "1234",
                "Mastercard",
                new Money(5000),
                day,
                15
            );

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(31)]
    public void CreditCard_With_Invalid_DueDay_Should_Fail(int day)
    {
        Action act = () =>
            new CreditCard(
                Guid.NewGuid(),
                "Nubank",
                "1234",
                "Mastercard",
                new Money(5000),
                10,
                day
            );

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deactivating_CreditCard_Should_Not_Change_Identity()
    {
        var card = CreateValidCreditCard();
        var id = card.Id;

        card.Deactivate();

        card.Id.Should().Be(id);
    }
}
