using FluentAssertions;
using PersonalBudget.Application.DTOs.Simulator;
using PersonalBudget.Application.Services.Simulator;

namespace PersonalBudget.Domain.Tests.Simulator;

public class ProjectionValidatorTests
{
    private static readonly DateTime ServerToday = new(2026, 10, 6, 21, 30, 0);

    private static ProjectionImpactInput Impact(
        string? description = "Carro",
        ImpactType type = ImpactType.Expense,
        ImpactMode mode = ImpactMode.Single,
        string? startMonth = "2026-11",
        decimal amount = 100m,
        ImpactAmountKind? kind = null,
        int? installments = null,
        int? months = null,
        string? id = "a") =>
        new(id, description, type, mode, startMonth, amount, kind, installments, months);

    private static ProjectionCommand Command(
        int months = 6, string? today = "2026-10-15", params ProjectionImpactInput[] impacts) =>
        new(Guid.NewGuid(), today, months, impacts);

    private static ValidatedProjection Validate(ProjectionCommand c) =>
        ProjectionValidator.Validate(c, ServerToday);

    private static void ShouldFail(ProjectionCommand c, string messagePart)
    {
        Action act = () => Validate(c);
        act.Should().Throw<DomainException>().Which.Message.Should().Contain(messagePart);
    }

    [Fact]
    public void ValidRequest_IsNormalized()
    {
        var v = Validate(Command(3, "2026-10-15", Impact(mode: ImpactMode.Installment, installments: 12, kind: ImpactAmountKind.Total)));

        v.Today.Should().Be(new DateTime(2026, 10, 15));
        v.Reference.Should().Be(new YearMonth(2026, 10));
        v.Months.Should().Be(3);
        var i = v.Impacts.Single();
        i.Id.Should().Be("a");
        i.Installments.Should().Be(12);
        i.AmountKind.Should().Be(ImpactAmountKind.Total);
        i.StartMonth.Should().Be(new YearMonth(2026, 11));
    }

    [Fact]
    public void MissingToday_FallsBackToTheGivenServerDate()
    {
        var v = Validate(Command(today: null));

        v.Today.Should().Be(new DateTime(2026, 10, 6));
        v.Reference.Should().Be(new YearMonth(2026, 10));
    }

    [Fact]
    public void MissingImpacts_MeansNoImpacts()
    {
        var v = Validate(new ProjectionCommand(Guid.NewGuid(), "2026-10-15", 3, null));

        v.Impacts.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(-1)]
    public void Horizon_OutsideOneToTwentyFour_Fails(int months) =>
        ShouldFail(Command(months), "entre 1 e 24");

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    public void Horizon_AtTheLimits_Passes(int months) =>
        Validate(Command(months)).Months.Should().Be(months);

    [Theory]
    [InlineData("15/10/2026")]
    [InlineData("2026-10")]
    [InlineData("hoje")]
    public void InvalidToday_Fails(string today) =>
        ShouldFail(Command(today: today), "yyyy-MM-dd");

    [Fact]
    public void MoreThanFiftyImpacts_Fails() =>
        ShouldFail(Command(impacts: Enumerable.Repeat(Impact(), 51).ToArray()), "No máximo 50");

    [Fact]
    public void FiftyImpacts_Pass() =>
        Validate(Command(impacts: Enumerable.Repeat(Impact(), 50).ToArray())).Impacts.Should().HaveCount(50);

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AmountNotPositive_FailsNamingTheImpact(int amount) =>
        ShouldFail(Command(impacts: [Impact(), Impact(description: "Viagem", amount: amount)]),
            "Impacto #2 (\"Viagem\"): o valor deve ser maior que zero");

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    [InlineData(-3)]
    public void InstallmentsOutsideOneTo120_Fails(int installments) =>
        ShouldFail(Command(impacts: [Impact(mode: ImpactMode.Installment, installments: installments)]),
            "Impacto #1 (\"Carro\"): o número de parcelas deve estar entre 1 e 120");

    [Fact]
    public void InstallmentWithoutCount_Fails() =>
        ShouldFail(Command(impacts: [Impact(mode: ImpactMode.Installment, installments: null)]), "parcelas");

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    public void InstallmentsAtTheLimits_Pass(int installments) =>
        Validate(Command(impacts: [Impact(mode: ImpactMode.Installment, installments: installments)]))
            .Impacts.Single().Installments.Should().Be(installments);

    [Fact]
    public void InstallmentsAreIgnoredOutsideInstallmentMode() =>
        Validate(Command(impacts: [Impact(mode: ImpactMode.Single, installments: 999)]))
            .Impacts.Should().ContainSingle();

    [Theory]
    [InlineData("2026-13")]
    [InlineData("2026-1")]
    [InlineData("10/2026")]
    [InlineData("2026-10-01")]
    [InlineData("1999-12")]
    [InlineData("2101-01")]
    [InlineData("")]
    [InlineData(null)]
    public void InvalidStartMonth_Fails(string? startMonth) =>
        ShouldFail(Command(impacts: [Impact(startMonth: startMonth)]), "mês de início inválido");

    [Fact]
    public void UndefinedEnumValues_Fail()
    {
        ShouldFail(Command(impacts: [Impact(type: (ImpactType)99)]), "tipo inválido");
        ShouldFail(Command(impacts: [Impact(mode: (ImpactMode)0)]), "modo inválido");
        ShouldFail(Command(impacts: [Impact(kind: (ImpactAmountKind)7)]), "tipo de valor inválido");
    }

    [Fact]
    public void DescriptionLongerThan120_Fails() =>
        ShouldFail(Command(impacts: [Impact(description: new string('x', 121))]), "no máximo 120");

    [Fact]
    public void Description120_Passes() =>
        Validate(Command(impacts: [Impact(description: new string('x', 120))])).Impacts.Should().ContainSingle();

    [Fact]
    public void NullImpact_Fails() =>
        ShouldFail(Command(impacts: [null!]), "Impacto #1");

    [Fact]
    public void BlankDescription_IsNamedByPosition() =>
        ShouldFail(Command(impacts: [Impact(description: "  ", amount: 0m)]), "Impacto #1:");

    [Theory]
    [InlineData(-1)]
    [InlineData(121)]
    public void MonthlyDurationOutOfRange_Fails(int months) =>
        ShouldFail(Command(impacts: [Impact(mode: ImpactMode.Monthly, months: months)]), "duração mensal");

    [Fact]
    public void MonthlyDuration_ZeroOrNull_MeansUntilTheEndOfTheHorizon()
    {
        Validate(Command(impacts: [Impact(mode: ImpactMode.Monthly, months: 0)])).Impacts.Single().Months.Should().BeNull();
        Validate(Command(impacts: [Impact(mode: ImpactMode.Monthly, months: null)])).Impacts.Single().Months.Should().BeNull();
        Validate(Command(impacts: [Impact(mode: ImpactMode.Monthly, months: 6)])).Impacts.Single().Months.Should().Be(6);
    }

    [Fact]
    public void MissingId_GetsAStableOne() =>
        Validate(Command(impacts: [Impact(id: null), Impact(id: " ")])).Impacts.Select(i => i.Id)
            .Should().Equal("impact-1", "impact-2");

    [Fact]
    public void MissingAmountKind_DefaultsToPerInstallment() =>
        Validate(Command(impacts: [Impact(mode: ImpactMode.Installment, installments: 3, kind: null)]))
            .Impacts.Single().AmountKind.Should().Be(ImpactAmountKind.PerInstallment);
}
