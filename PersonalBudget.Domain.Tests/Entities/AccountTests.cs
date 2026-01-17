public class AccountTests
{
    [Fact]
    public void Should_not_create_account_without_name()
    {
        Assert.Throws<DomainException>(() =>
            new Account("", new Money(100)));
    }

    [Fact]
    public void Should_credit_account()
    {
        var account = new Account("Main", new Money(100));

        account.Credit(new Money(50));

        Assert.Equal(150, account.Balance.Amount);
    }

    [Fact]
    public void Should_debit_account()
    {
        var account = new Account("Main", new Money(100));

        account.Debit(new Money(40));

        Assert.Equal(60, account.Balance.Amount);
    }

    [Fact]
    public void Should_not_allow_debit_more_than_balance()
    {
        var account = new Account("Main", new Money(100));

        Assert.Throws<DomainException>(() =>
            account.Debit(new Money(200)));
    }
}
