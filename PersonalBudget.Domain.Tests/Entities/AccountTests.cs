using System;
using Xunit;

public class AccountTests
{
    private static Account CreateValidAccount(
        decimal initialBalance = 100)
    {
        return new Account(
            userId: Guid.NewGuid(),
            bank: Bank.Nubank,
            agency: new BankAgency("0001"),
            number: new BankAccountNumber("123456-7"),
            initialBalance: new Money(initialBalance)
        );
    }

    [Fact]
    public void Should_create_account_with_valid_data()
    {
        var account = CreateValidAccount();

        Assert.Equal(Bank.Nubank, account.Bank);
        Assert.Equal("0001", account.Agency.Value);
        Assert.Equal("123456-7", account.Number.Value);
        Assert.Equal(100, account.Balance.Amount);
        Assert.NotEqual(Guid.Empty, account.UserId);
    }

    [Fact]
    public void Should_not_create_account_without_user()
    {
        Assert.Throws<DomainException>(() =>
            new Account(
                Guid.Empty,
                Bank.Itau,
                new BankAgency("0001"),
                new BankAccountNumber("12345"),
                new Money(100)
            ));
    }

    [Fact]
    public void Should_credit_account_balance()
    {
        var account = CreateValidAccount(100);

        account.Credit(new Money(50));

        Assert.Equal(150, account.Balance.Amount);
    }

    [Fact]
    public void Should_debit_account_balance()
    {
        var account = CreateValidAccount(100);

        account.Debit(new Money(40));

        Assert.Equal(60, account.Balance.Amount);
    }

    [Fact]
    public void Should_not_allow_debit_more_than_balance()
    {
        var account = CreateValidAccount(100);

        Assert.Throws<DomainException>(() =>
            account.Debit(new Money(200)));
    }

    [Fact]
    public void Should_not_allow_negative_initial_balance()
    {
        Assert.Throws<DomainException>(() =>
            CreateValidAccount(-10));
    }
}
