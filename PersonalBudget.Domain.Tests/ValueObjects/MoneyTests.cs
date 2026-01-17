public class MoneyTests
{
    [Fact]
    public void Should_create_money_with_positive_value()
    {
        var money = new Money(100);

        Assert.Equal(100, money.Amount);
    }

    [Fact]
    public void Should_not_allow_zero_or_negative_value()
    {
        Assert.Throws<DomainException>(() => new Money(0));
        Assert.Throws<DomainException>(() => new Money(-10));
    }

    [Fact]
    public void Should_add_money_correctly()
    {
        var a = new Money(100);
        var b = new Money(50);

        var result = a.Add(b);

        Assert.Equal(150, result.Amount);
    }

    [Fact]
    public void Should_not_allow_negative_result_on_subtract()
    {
        var a = new Money(50);
        var b = new Money(100);

        Assert.Throws<DomainException>(() => a.Subtract(b));
    }
}
