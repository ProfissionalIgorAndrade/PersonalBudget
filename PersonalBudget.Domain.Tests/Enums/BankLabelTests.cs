using FluentAssertions;

namespace PersonalBudget.Domain.Tests.Enums;

public class BankLabelTests
{
    public static TheoryData<Bank> AllBanks
    {
        get
        {
            var data = new TheoryData<Bank>();
            foreach (var bank in Enum.GetValues<Bank>())
                data.Add(bank);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(AllBanks))]
    public void Of_EveryBank_ReturnsNonEmptyLabel(Bank bank)
    {
        BankLabel.Of(bank).Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(Bank.Itau, "Itau")]
    [InlineData(Bank.Nubank, "Nubank")]
    [InlineData(Bank.Inter, "Inter")]
    [InlineData(Bank.Santander, "Santander")]
    [InlineData(Bank.Bradesco, "Bradesco")]
    [InlineData(Bank.Caixa, "Caixa")]
    [InlineData(Bank.BancoDoBrasil, "Banco do Brasil")]
    [InlineData(Bank.Btg, "BTG Pactual")]
    [InlineData(Bank.C6Bank, "C6 Bank")]
    [InlineData(Bank.Safra, "Safra")]
    [InlineData(Bank.Sicoob, "Sicoob")]
    [InlineData(Bank.Sicredi, "Sicredi")]
    [InlineData(Bank.Original, "Banco Original")]
    [InlineData(Bank.Pan, "Banco Pan")]
    [InlineData(Bank.Neon, "Neon")]
    [InlineData(Bank.PicPay, "PicPay")]
    [InlineData(Bank.MercadoPago, "Mercado Pago")]
    [InlineData(Bank.Banrisul, "Banrisul")]
    [InlineData(Bank.Next, "Next")]
    [InlineData(Bank.Bmg, "Banco BMG")]
    [InlineData(Bank.Xp, "XP")]
    [InlineData(Bank.PagBank, "PagBank")]
    [InlineData(Bank.Bv, "Banco BV")]
    [InlineData(Bank.Outro, "Outro")]
    public void Of_ReturnsTheExpectedLabel(Bank bank, string expected)
    {
        BankLabel.Of(bank).Should().Be(expected);
    }

    [Fact]
    public void Of_ValueOutsideTheEnum_FallsBackToTheNumber()
    {
        BankLabel.Of((Bank)99).Should().Be("99");
    }
}
