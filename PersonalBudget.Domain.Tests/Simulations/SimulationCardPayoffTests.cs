using FluentAssertions;
using PersonalBudget.Application.DTOs.Simulations;
using PersonalBudget.Application.Services;

namespace PersonalBudget.Domain.Tests.Simulations;

/// <summary>Regras do modo <see cref="SimulationMode.CardPayoff"/> (quitar parcelas de cartão).</summary>
public class SimulationCardPayoffTests
{
    private static readonly Guid Household = Guid.NewGuid();
    private static readonly Guid Owner = Guid.NewGuid();

    private static Simulation Payoff(
        IReadOnlyCollection<Guid>? cards = null,
        SimulationType type = SimulationType.Expense,
        decimal amount = 0m,
        int? installments = null,
        int? months = null) =>
        Simulation.Create(Household, Owner, "Zerar cartões", type, SimulationMode.CardPayoff, "2026-12",
            amount, SimulationAmountKind.Total, installments, months, creditCardIds: cards);

    private static void ShouldFail(Func<Simulation> act, string messagePart) =>
        act.Should().Throw<DomainException>().Which.Message.Should().ContainEquivalentOf(messagePart);

    [Fact]
    public void Create_WithoutCards_MeansAllCards()
    {
        var s = Payoff();

        s.Mode.Should().Be(SimulationMode.CardPayoff);
        s.StartMonth.Should().Be("2026-12");
        s.CreditCardIds.Should().BeNull("nulo = todos os cartões do lar, inclusive os criados depois");
    }

    [Fact]
    public void Create_WithCards_KeepsThemInOrder()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        Payoff([a, b]).CreditCardIds.Should().Equal(a, b);
    }

    [Fact]
    public void Create_IgnoresAmountAndOtherModeFields()
    {
        var s = Payoff(amount: 999m, installments: 3, months: 4);

        s.Amount.Should().Be(0m, "o valor da quitação é calculado a partir das parcelas");
        s.AmountKind.Should().Be(SimulationAmountKind.PerInstallment);
        s.Installments.Should().BeNull();
        s.Months.Should().BeNull();
    }

    [Fact]
    public void Create_WithZeroAmount_IsAccepted()
        => Payoff(amount: 0m).Amount.Should().Be(0m);

    [Fact]
    public void Create_AsIncome_Fails()
        => ShouldFail(() => Payoff(type: SimulationType.Income), "sempre uma despesa");

    [Fact]
    public void Create_WithEmptyCardList_Fails()
        => ShouldFail(() => Payoff([]), "ao menos um cartão");

    [Fact]
    public void Create_WithEmptyGuid_Fails()
        => ShouldFail(() => Payoff([Guid.Empty]), "cartão inválido");

    [Fact]
    public void Create_WithRepeatedCard_Fails()
    {
        var a = Guid.NewGuid();
        ShouldFail(() => Payoff([a, a]), "repetidos");
    }

    [Fact]
    public void Create_WithTooManyCards_Fails()
        => ShouldFail(
            () => Payoff(Enumerable.Range(0, SimulationRules.MaxCreditCards + 1).Select(_ => Guid.NewGuid()).ToList()),
            $"no máximo {SimulationRules.MaxCreditCards} cartões");

    [Fact]
    public void Create_OtherModes_DropCardIds()
    {
        var s = Simulation.Create(Household, Owner, "Carro", SimulationType.Expense, SimulationMode.Single, "2026-12",
            100m, SimulationAmountKind.PerInstallment, null, null, creditCardIds: [Guid.NewGuid()]);

        s.CreditCardIds.Should().BeNull("cartões só valem na quitação");
    }

    [Fact]
    public void Update_FromPayoffToSingle_ClearsCards_AndBack()
    {
        var card = Guid.NewGuid();
        var s = Payoff([card]);

        s.Update("Carro", SimulationType.Expense, SimulationMode.Single, "2026-12", 100m,
            SimulationAmountKind.PerInstallment, null, null);
        s.CreditCardIds.Should().BeNull();
        s.Amount.Should().Be(100m);

        s.Update("Zerar", SimulationType.Expense, SimulationMode.CardPayoff, "2027-01", 0m,
            SimulationAmountKind.PerInstallment, null, null, [card]);
        s.CreditCardIds.Should().Equal(card);
        s.Amount.Should().Be(0m);
        s.StartMonth.Should().Be("2027-01");
    }

    [Fact]
    public void Validator_PrefixesPayoffErrors()
    {
        var input = new SimulationInput("Zerar", SimulationType.Expense, SimulationMode.CardPayoff, "2026-12", 0m,
            null, null, null, []);

        Action act = () => SimulationValidator.Validate(input, 2);

        act.Should().Throw<DomainException>().Which.Message.Should()
            .Be("Simulação #2 (\"Zerar\"): escolha ao menos um cartão, ou envie creditCardIds nulo para todos.");
    }

    [Fact]
    public void Validator_AcceptsPayoffWithoutAmount()
    {
        var input = new SimulationInput("Zerar", SimulationType.Expense, SimulationMode.CardPayoff, "2026-12", 0m,
            null, null, null, null);

        Action act = () => SimulationValidator.Validate(input);

        act.Should().NotThrow();
    }
}
