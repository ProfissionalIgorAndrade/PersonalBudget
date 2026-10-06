using FluentAssertions;

namespace PersonalBudget.Domain.Tests.Enums;

/// <summary>
/// Os inteiros de Bank são gravados em accounts.bank e os nomes são o contrato da API:
/// renumerar ou renomear corrompe dados existentes.
/// </summary>
public class BankTests
{
    public static TheoryData<string, int> Pinned => new()
    {
        { "Itau", 1 }, { "Nubank", 2 }, { "Inter", 3 }, { "Santander", 4 },
        { "Bradesco", 5 }, { "Caixa", 6 }, { "BancoDoBrasil", 7 }, { "Btg", 8 },
        { "C6Bank", 9 }, { "Safra", 10 }, { "Sicoob", 11 }, { "Sicredi", 12 },
        { "Original", 13 }, { "Pan", 14 }, { "Neon", 15 }, { "PicPay", 16 },
        { "MercadoPago", 17 }, { "Banrisul", 18 }, { "Next", 19 }, { "Bmg", 20 },
        { "Xp", 21 }, { "PagBank", 22 }, { "Bv", 23 }, { "Outro", 24 }
    };

    [Theory]
    [MemberData(nameof(Pinned))]
    public void Member_KeepsItsPersistedValue(string name, int value)
    {
        Enum.Parse<Bank>(name).Should().Be((Bank)value);
        ((int)Enum.Parse<Bank>(name)).Should().Be(value);
    }

    [Fact]
    public void Enum_HasNoMemberOutsideThePinnedList()
    {
        Enum.GetNames<Bank>().Should().BeEquivalentTo(
            Pinned.Select(p => (string)p[0]));
    }
}
