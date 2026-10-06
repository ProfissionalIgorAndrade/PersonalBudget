using FluentAssertions;
using PersonalBudget.Application.DTOs.Simulator;
using PersonalBudget.Application.Services.Simulator;

namespace PersonalBudget.Domain.Tests.Simulator;

public class ImpactScheduleTests
{
    // Referência out/26, horizonte de 6 meses: out/26 nov/26 dez/26 jan/27 fev/27 mar/27.
    private static readonly YearMonth Reference = new(2026, 10);
    private const int Horizon = 6;

    private static ImpactDefinition Def(
        ImpactMode mode,
        string start = "2026-10",
        decimal amount = 100m,
        ImpactType type = ImpactType.Expense,
        ImpactAmountKind kind = ImpactAmountKind.PerInstallment,
        int installments = 1,
        int? months = null)
    {
        YearMonth.TryParse(start, out var startMonth).Should().BeTrue();
        return new ImpactDefinition("i1", "Teste", type, mode, startMonth, amount, kind, installments, months);
    }

    private static ScheduledImpact Build(ImpactDefinition def, int horizon = Horizon) =>
        ImpactSchedule.Build(def, Reference, horizon);

    // ─── installment amounts ────────────────────────────────────────────────

    [Fact]
    public void Installment_Total_PutsTheRemainderOnTheLastInstallment()
    {
        var s = Build(Def(ImpactMode.Installment, amount: 100m, kind: ImpactAmountKind.Total, installments: 3));

        s.Projection.InstallmentAmount.Should().Be(33.33m);
        s.Projection.LastInstallmentAmount.Should().Be(33.34m);
        s.Projection.Monthly.Should().Equal(-33.33m, -33.33m, -33.34m, 0m, 0m, 0m);
        s.Projection.TotalFull.Should().Be(-100m);
        s.Projection.TotalInHorizon.Should().Be(-100m);
        s.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Installment_Total_TwelveInstallments_LastAbsorbsTheRoundingDifference()
    {
        var s = Build(Def(ImpactMode.Installment, amount: 1000m, kind: ImpactAmountKind.Total, installments: 12), horizon: 12);

        s.Projection.InstallmentAmount.Should().Be(83.33m);
        s.Projection.LastInstallmentAmount.Should().Be(83.37m);
        s.Projection.Monthly[10].Should().Be(-83.33m);
        s.Projection.Monthly[11].Should().Be(-83.37m);
        s.Projection.TotalFull.Should().Be(-1000m);
    }

    [Fact]
    public void Installment_Total_SingleInstallment_IsTheWholeAmount()
    {
        var s = Build(Def(ImpactMode.Installment, amount: 99.99m, kind: ImpactAmountKind.Total, installments: 1));

        s.Projection.InstallmentAmount.Should().Be(99.99m);
        s.Projection.LastInstallmentAmount.Should().Be(99.99m);
        s.Projection.TotalFull.Should().Be(-99.99m);
    }

    [Fact]
    public void Installment_PerInstallment_UsesTheGivenAmountOnEveryInstallment()
    {
        var s = Build(Def(ImpactMode.Installment, amount: 150m, kind: ImpactAmountKind.PerInstallment, installments: 4));

        s.Projection.InstallmentAmount.Should().Be(150m);
        s.Projection.LastInstallmentAmount.Should().Be(150m);
        s.Projection.Monthly.Should().Equal(-150m, -150m, -150m, -150m, 0m, 0m);
        s.Projection.TotalFull.Should().Be(-600m);
        s.Projection.InstallmentsTotal.Should().Be(4);
        s.Projection.InstallmentsInHorizon.Should().Be(4);
    }

    [Fact]
    public void Income_IsPositiveAndExpenseIsNegative()
    {
        var income = Build(Def(ImpactMode.Single, type: ImpactType.Income, amount: 500m));
        var expense = Build(Def(ImpactMode.Single, type: ImpactType.Expense, amount: 500m));

        income.Projection.Monthly[0].Should().Be(500m);
        expense.Projection.Monthly[0].Should().Be(-500m);
    }

    // ─── single and monthly ─────────────────────────────────────────────────

    [Fact]
    public void Single_LandsOnlyOnItsStartMonth()
    {
        var s = Build(Def(ImpactMode.Single, start: "2026-12", amount: 500m));

        s.Projection.Monthly.Should().Equal(0m, 0m, -500m, 0m, 0m, 0m);
        s.Projection.TotalInHorizon.Should().Be(-500m);
        s.Projection.TotalFull.Should().Be(-500m);
        s.Projection.InstallmentAmount.Should().BeNull();
        s.Projection.InstallmentsTotal.Should().Be(1);
        s.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Monthly_WithoutDuration_RunsUntilTheEndOfTheHorizon()
    {
        var s = Build(Def(ImpactMode.Monthly, start: "2026-11", amount: 300m));

        s.Projection.Monthly.Should().Equal(0m, -300m, -300m, -300m, -300m, -300m);
        s.Projection.TotalInHorizon.Should().Be(-1500m);
        s.Projection.TotalFull.Should().Be(-1500m);
        s.Projection.InstallmentsTotal.Should().BeNull();
        s.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Monthly_ZeroDuration_BehavesLikeNoDuration()
    {
        // O validador normaliza 0 para null; o cálculo também trata 0 como "sem duração".
        var s = Build(Def(ImpactMode.Monthly, start: "2026-10", amount: 300m, months: 0));

        s.Projection.InstallmentsInHorizon.Should().Be(6);
        s.Projection.TotalFull.Should().Be(-1800m);
    }

    [Fact]
    public void Monthly_WithDuration_StopsAfterTheGivenMonths()
    {
        var s = Build(Def(ImpactMode.Monthly, start: "2026-10", amount: 300m, months: 2));

        s.Projection.Monthly.Should().Equal(-300m, -300m, 0m, 0m, 0m, 0m);
        s.Projection.TotalFull.Should().Be(-600m);
        s.Projection.InstallmentsTotal.Should().Be(2);
        s.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Monthly_DurationPastTheHorizon_KeepsTheFullTotalAndWarns()
    {
        var s = Build(Def(ImpactMode.Monthly, start: "2027-02", amount: 300m, months: 3));

        s.Projection.Monthly.Should().Equal(0m, 0m, 0m, 0m, -300m, -300m);
        s.Projection.TotalInHorizon.Should().Be(-600m);
        s.Projection.TotalFull.Should().Be(-900m);
        s.Projection.InstallmentsInHorizon.Should().Be(2);
        s.Projection.InstallmentsTotal.Should().Be(3);
        s.Warnings.Should().ContainSingle().Which.Code.Should().Be(ProjectionWarningCode.AfterWindow);
    }

    // ─── calendar ───────────────────────────────────────────────────────────

    [Fact]
    public void Installment_CrossesTheYearBoundary_NovemberToFebruary()
    {
        var s = Build(Def(ImpactMode.Installment, start: "2026-11", amount: 200m, installments: 4));

        // nov/26 dez/26 jan/27 fev/27 -> índices 1..4
        s.Projection.Monthly.Should().Equal(0m, -200m, -200m, -200m, -200m, 0m);
        s.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void YearMonth_AddMonths_RollsOverTheYearBothWays()
    {
        new YearMonth(2026, 11).AddMonths(3).Should().Be(new YearMonth(2027, 2));
        new YearMonth(2026, 1).AddMonths(-1).Should().Be(new YearMonth(2025, 12));
        new YearMonth(2026, 10).MonthsUntil(new YearMonth(2027, 3)).Should().Be(5);
    }

    [Fact]
    public void YearMonth_LabelIsLowercasePtBr()
    {
        new YearMonth(2026, 10).Label.Should().Be("out/26");
        new YearMonth(2027, 2).Label.Should().Be("fev/27");
        new YearMonth(2026, 10).ToString().Should().Be("2026-10");
    }

    [Theory]
    [InlineData("2026-10", true)]
    [InlineData("2026-1", false)]
    [InlineData("2026-13", false)]
    [InlineData("10/2026", false)]
    [InlineData("2026-10-01", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void YearMonth_TryParse_AcceptsOnlyYyyyMm(string? raw, bool expected)
    {
        YearMonth.TryParse(raw, out _).Should().Be(expected);
    }

    // ─── window and warnings ────────────────────────────────────────────────

    [Fact]
    public void Installment_StartingBeforeTheWindow_IsTruncatedAndKeepsTheFullTotal()
    {
        // jul/26 .. jun/27: 12 parcelas; a janela pega out/26..mar/27 (6 delas).
        var s = Build(Def(ImpactMode.Installment, start: "2026-07", amount: 100m, installments: 12));

        s.Projection.Monthly.Should().Equal(-100m, -100m, -100m, -100m, -100m, -100m);
        s.Projection.InstallmentsInHorizon.Should().Be(6);
        s.Projection.InstallmentsTotal.Should().Be(12);
        s.Projection.TotalInHorizon.Should().Be(-600m);
        s.Projection.TotalFull.Should().Be(-1200m);
        s.Warnings.Select(w => w.Code).Should().BeEquivalentTo(
            new[] { ProjectionWarningCode.Truncated, ProjectionWarningCode.AfterWindow });
    }

    [Fact]
    public void Installment_EndingBeforeTheWindow_ProducesBeforeWindowAndNoMonthlyAmounts()
    {
        // mai/26 jun/26 jul/26: tudo antes de out/26.
        var s = Build(Def(ImpactMode.Installment, start: "2026-05", amount: 100m, installments: 3));

        s.Projection.Monthly.Should().OnlyContain(v => v == 0m);
        s.Projection.TotalInHorizon.Should().Be(0m);
        s.Projection.TotalFull.Should().Be(-300m);
        s.Projection.InstallmentsInHorizon.Should().Be(0);
        s.Warnings.Should().ContainSingle().Which.Code.Should().Be(ProjectionWarningCode.BeforeWindow);
    }

    [Fact]
    public void Single_AfterTheHorizon_OnlyCountsInTheFullTotalAndWarns()
    {
        var s = Build(Def(ImpactMode.Single, start: "2027-06", amount: 800m));

        s.Projection.Monthly.Should().OnlyContain(v => v == 0m);
        s.Projection.TotalInHorizon.Should().Be(0m);
        s.Projection.TotalFull.Should().Be(-800m);
        s.Warnings.Should().ContainSingle().Which.Code.Should().Be(ProjectionWarningCode.AfterWindow);
    }

    [Fact]
    public void Installment_StartingRightAfterTheHorizon_WarnsAfterWindow()
    {
        var s = Build(Def(ImpactMode.Installment, start: "2027-04", amount: 100m, installments: 2));

        s.Projection.InstallmentsInHorizon.Should().Be(0);
        s.Projection.TotalFull.Should().Be(-200m);
        s.Warnings.Should().ContainSingle().Which.Code.Should().Be(ProjectionWarningCode.AfterWindow);
    }

    [Fact]
    public void Monthly_WithoutDuration_StartingAfterTheHorizon_WarnsAndIsEmpty()
    {
        var s = Build(Def(ImpactMode.Monthly, start: "2027-05", amount: 300m));

        s.Projection.Monthly.Should().OnlyContain(v => v == 0m);
        s.Projection.TotalFull.Should().Be(0m);
        s.Warnings.Should().ContainSingle().Which.Code.Should().Be(ProjectionWarningCode.AfterWindow);
    }

    [Fact]
    public void Monthly_WithoutDuration_StartingBeforeTheWindow_IsTruncatedAndCountsUntilTheHorizonEnd()
    {
        // ago/26 .. mar/27 = 8 meses; 6 dentro da janela.
        var s = Build(Def(ImpactMode.Monthly, start: "2026-08", amount: 100m));

        s.Projection.TotalInHorizon.Should().Be(-600m);
        s.Projection.TotalFull.Should().Be(-800m);
        s.Warnings.Should().ContainSingle().Which.Code.Should().Be(ProjectionWarningCode.Truncated);
    }

    [Fact]
    public void Warnings_CarryTheImpactIdAndNameTheImpact()
    {
        var s = Build(Def(ImpactMode.Single, start: "2027-06"));

        var warning = s.Warnings.Single();
        warning.ImpactId.Should().Be("i1");
        warning.Message.Should().Contain("Teste").And.Contain("mar/27");
    }

    [Fact]
    public void ShortHorizon_OneMonth_OnlyTheReferenceMonthIsInside()
    {
        var s = Build(Def(ImpactMode.Installment, start: "2026-10", amount: 100m, installments: 3), horizon: 1);

        s.Projection.Monthly.Should().Equal(-100m);
        s.Projection.InstallmentsInHorizon.Should().Be(1);
        s.Projection.TotalFull.Should().Be(-300m);
        s.Warnings.Should().ContainSingle().Which.Code.Should().Be(ProjectionWarningCode.AfterWindow);
    }
}
