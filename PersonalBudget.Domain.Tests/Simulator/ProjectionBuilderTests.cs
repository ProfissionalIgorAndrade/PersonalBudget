using FluentAssertions;
using PersonalBudget.Application.DTOs.Simulator;
using PersonalBudget.Application.Services.Simulator;

namespace PersonalBudget.Domain.Tests.Simulator;

public class ProjectionBuilderTests
{
    // Hoje = 15/out/26, horizonte out/nov/dez. Janela da média: jul, ago, set.
    private static readonly DateTime Today = new(2026, 10, 15);
    private static readonly YearMonth Reference = new(2026, 10);

    private static IReadOnlyList<OpeningAccountDto> Accounts(decimal balance) =>
        [new OpeningAccountDto(Guid.NewGuid(), "Nubank - Igor", balance)];

    private static ProjectionFlowRow Row(
        int year, int month, bool card, TransactionType type, TransactionFrequency frequency,
        decimal total, decimal posted = 0m) =>
        new(year, month, card, type, frequency, total, posted);

    private static ImpactDefinition Impact(
        ImpactMode mode, string start, decimal amount, ImpactType type = ImpactType.Expense,
        int? months = null, string id = "i1")
    {
        YearMonth.TryParse(start, out var startMonth).Should().BeTrue();
        return new ImpactDefinition(id, "Teste", type, mode, startMonth, amount,
            ImpactAmountKind.PerInstallment, 1, months);
    }

    /// <summary>
    /// Set/26: receita 5000, variável 2000, fixa 1000. Ago/26: receita 3000, variável 1000. Jul/26: sem dados.
    /// => média de receita 4000 e de variável 1500, sobre 2 meses com dados.
    /// Out/26 (mês atual): salário 4000 e aluguel 800 já lançados antes de hoje, variável 300 (200 até hoje),
    /// parcela de cartão 150 e variável de cartão 50.
    /// Nov/26: parcela de cartão 150 e fixa futura 800.
    /// </summary>
    private static List<ProjectionFlowRow> Rows() =>
    [
        Row(2026, 9, false, TransactionType.Income, TransactionFrequency.Fixed, 5000m, 5000m),
        Row(2026, 9, false, TransactionType.Expense, TransactionFrequency.Variable, 2000m, 2000m),
        Row(2026, 9, false, TransactionType.Expense, TransactionFrequency.Fixed, 1000m, 1000m),
        Row(2026, 8, false, TransactionType.Income, TransactionFrequency.Fixed, 3000m, 3000m),
        Row(2026, 8, false, TransactionType.Expense, TransactionFrequency.Variable, 1000m, 1000m),

        Row(2026, 10, false, TransactionType.Income, TransactionFrequency.Fixed, 4000m, 4000m),
        Row(2026, 10, false, TransactionType.Expense, TransactionFrequency.Fixed, 800m, 800m),
        Row(2026, 10, false, TransactionType.Expense, TransactionFrequency.Variable, 300m, 200m),
        Row(2026, 10, true, TransactionType.Expense, TransactionFrequency.Installments, 150m),
        Row(2026, 10, true, TransactionType.Expense, TransactionFrequency.Variable, 50m),

        Row(2026, 11, true, TransactionType.Expense, TransactionFrequency.Installments, 150m),
        Row(2026, 11, false, TransactionType.Expense, TransactionFrequency.Fixed, 800m),
    ];

    private static ProjectionResponse Build(
        decimal opening = 1000m, IReadOnlyList<ProjectionFlowRow>? rows = null,
        IReadOnlyList<ImpactDefinition>? impacts = null) =>
        ProjectionBuilder.Build(Reference, Today, 3, Accounts(opening), rows ?? Rows(),
            impacts ?? Array.Empty<ImpactDefinition>());

    // ─── averages ───────────────────────────────────────────────────────────

    [Fact]
    public void Averages_IgnoreMonthsWithoutData()
    {
        var r = Build();

        r.Assumptions.LookbackMonths.Should().Be(3);
        r.Assumptions.MonthsWithData.Should().Be(2);
        r.Assumptions.AverageIncome.Should().Be(4000m);
        r.Assumptions.AverageVariableExpense.Should().Be(1500m);
        r.Assumptions.Notes.Should().NotBeEmpty();
    }

    [Fact]
    public void Averages_DoNotUseTheReferenceMonthNorFutureMonths()
    {
        var (income, variable, months) = ProjectionBuilder.ComputeAverages(Rows(), Reference);

        income.Should().Be(4000m);
        variable.Should().Be(1500m);
        months.Should().Be(2);
    }

    [Fact]
    public void Averages_AreRoundedToTwoDecimals()
    {
        var rows = new List<ProjectionFlowRow>
        {
            Row(2026, 9, false, TransactionType.Income, TransactionFrequency.Variable, 100m),
            Row(2026, 8, false, TransactionType.Income, TransactionFrequency.Variable, 100m),
            Row(2026, 7, false, TransactionType.Income, TransactionFrequency.Variable, 101m),
        };

        ProjectionBuilder.ComputeAverages(rows, Reference).AverageIncome.Should().Be(100.33m);
    }

    [Fact]
    public void NoHistory_AveragesAreNullAndEstimatesAreZero()
    {
        var r = Build(opening: 700m, rows: new List<ProjectionFlowRow>());

        r.Assumptions.AverageIncome.Should().BeNull();
        r.Assumptions.AverageVariableExpense.Should().BeNull();
        r.Assumptions.MonthsWithData.Should().Be(0);
        r.Baseline.Should().OnlyContain(m => m.Income == 0m && m.Committed == 0m && m.Variable == 0m && m.Result == 0m);
        r.Baseline.Should().OnlyContain(m => m.Balance == 700m);
    }

    // ─── baseline ───────────────────────────────────────────────────────────

    [Fact]
    public void ReferenceMonth_DoesNotDoubleCountWhatIsAlreadyInTheOpeningBalance()
    {
        var oct = Build().Baseline[0];

        // Salário e aluguel de antes de hoje já estão no saldo: não entram de novo.
        oct.Income.Should().Be(0m);
        // Só a parcela do cartão (a fatura inteira conta); o aluguel de conta já foi.
        oct.Committed.Should().Be(150m);
        // 100 de conta depois de hoje + 50 de cartão + resto até a média: 1500 - (300 + 50) = 1150.
        oct.Variable.Should().Be(1300m);
        oct.Result.Should().Be(-1450m);
        oct.Balance.Should().Be(-450m);
    }

    [Fact]
    public void FutureMonth_UsesPostedRowsAndCompletesIncomeAndVariableToTheAverage()
    {
        var nov = Build().Baseline[1];

        nov.Label.Should().Be("nov/26");
        nov.Income.Should().Be(4000m);
        nov.Committed.Should().Be(950m);
        nov.Variable.Should().Be(1500m);
        nov.Result.Should().Be(1550m);
        nov.Balance.Should().Be(1100m);
    }

    [Fact]
    public void FutureMonth_PostedVariableAboveTheAverage_IsNotReducedNorTopped()
    {
        var rows = Rows();
        rows.Add(Row(2026, 12, true, TransactionType.Expense, TransactionFrequency.Variable, 2500m));

        var dec = Build(rows: rows).Baseline[2];

        dec.Variable.Should().Be(2500m);
    }

    [Fact]
    public void FutureMonth_PostedIncomeAboveTheAverage_IsKept()
    {
        var rows = Rows();
        rows.Add(Row(2026, 12, false, TransactionType.Income, TransactionFrequency.Variable, 9000m));

        Build(rows: rows).Baseline[2].Income.Should().Be(9000m);
    }

    [Fact]
    public void Baseline_RunningBalanceStartsFromTheOpeningBalance()
    {
        var r = Build();

        r.OpeningBalance.Amount.Should().Be(1000m);
        r.OpeningBalance.AsOf.Should().Be("2026-10-15");
        r.OpeningBalance.ExcludesSavings.Should().BeTrue();
        r.ReferenceMonth.Should().Be("2026-10");
        r.Baseline.Select(b => b.Balance).Should().Equal(-450m, 1100m, 3600m);
        r.Baseline.Select(b => b.Label).Should().Equal("out/26", "nov/26", "dez/26");
    }

    [Fact]
    public void OpeningBalance_IsTheSumOfTheAccounts()
    {
        var accounts = new List<OpeningAccountDto>
        {
            new(Guid.NewGuid(), "A", 300m),
            new(Guid.NewGuid(), "B", -50m),
        };

        var r = ProjectionBuilder.Build(Reference, Today, 3, accounts, new List<ProjectionFlowRow>(), []);

        r.OpeningBalance.Amount.Should().Be(250m);
        r.OpeningBalance.Accounts.Should().HaveCount(2);
    }

    // ─── scenario ───────────────────────────────────────────────────────────

    [Fact]
    public void WithoutImpacts_ScenarioEqualsBaseline()
    {
        var r = Build();

        r.Scenario.Select(s => s.Balance).Should().Equal(r.Baseline.Select(b => b.Balance));
        r.Scenario.Should().OnlyContain(s => s.Delta == 0m && s.SimulatedIncome == 0m && s.SimulatedExpense == 0m);
        r.Summary.TotalImpactInHorizon.Should().Be(0m);
        r.Summary.TotalImpactFull.Should().Be(0m);
        r.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Scenario_AddsImpactsToTheBaselineAndTheDeltaAccumulates()
    {
        var r = Build(impacts: [Impact(ImpactMode.Single, "2026-11", 500m)]);

        r.Scenario[1].SimulatedExpense.Should().Be(500m);
        r.Scenario[1].SimulatedIncome.Should().Be(0m);
        r.Scenario[1].Result.Should().Be(1050m);
        r.Scenario.Select(s => s.Balance).Should().Equal(-450m, 600m, 3100m);
        r.Scenario.Select(s => s.Delta).Should().Equal(0m, -500m, -500m);
        r.Summary.TotalImpactInHorizon.Should().Be(-500m);
        r.Summary.TotalImpactFull.Should().Be(-500m);
    }

    [Fact]
    public void Scenario_SeveralImpactsAddUpIncomeAndExpenseSeparately()
    {
        var r = Build(impacts:
        [
            Impact(ImpactMode.Single, "2026-11", 500m, id: "a"),
            Impact(ImpactMode.Monthly, "2026-11", 100m, ImpactType.Income, id: "b"),
            Impact(ImpactMode.Single, "2026-11", 50m, id: "c"),
        ]);

        r.Scenario[1].SimulatedExpense.Should().Be(550m);
        r.Scenario[1].SimulatedIncome.Should().Be(100m);
        r.Scenario[2].SimulatedIncome.Should().Be(100m);
        r.Scenario[1].Delta.Should().Be(-450m);
        r.Scenario[2].Delta.Should().Be(-350m);
        r.Impacts.Select(i => i.Id).Should().Equal("a", "b", "c");
    }

    // ─── summary ────────────────────────────────────────────────────────────

    [Fact]
    public void Summary_BaselineMinBalanceAndFirstNegativeMonth()
    {
        var s = Build().Summary;

        s.BaselineMinBalance.Amount.Should().Be(-450m);
        s.BaselineMinBalance.MonthIndex.Should().Be(0);
        s.FirstNegativeMonthIndexBaseline.Should().Be(0);
        s.EndBalanceBaseline.Should().Be(3600m);
    }

    [Fact]
    public void Summary_ScenarioMinBalanceIsInTheMonthWhereItIsLowest()
    {
        // nov: 1550 - 2000 = -450 -> -900; dez: 2500 - 2000 = 500 -> -400.
        var s = Build(impacts: [Impact(ImpactMode.Monthly, "2026-11", 2000m)]).Summary;

        s.ScenarioMinBalance.Amount.Should().Be(-900m);
        s.ScenarioMinBalance.MonthIndex.Should().Be(1);
        s.EndBalanceScenario.Should().Be(-400m);
        s.EndBalanceBaseline.Should().Be(3600m);
        s.TotalImpactInHorizon.Should().Be(-4000m);
    }

    [Fact]
    public void Summary_FirstNegativeScenarioMonth_CanDifferFromTheBaseline()
    {
        // Saldo de partida alto: o baseline nunca fica negativo; o cenário fica em nov.
        var r = Build(opening: 5000m, impacts: [Impact(ImpactMode.Single, "2026-11", 9000m)]);

        r.Summary.FirstNegativeMonthIndexBaseline.Should().BeNull();
        r.Summary.FirstNegativeMonthIndexScenario.Should().Be(1);
        r.Summary.BaselineMinBalance.Amount.Should().Be(3550m);
        r.Summary.BaselineMinBalance.MonthIndex.Should().Be(0);
        r.Summary.ScenarioMinBalance.Amount.Should().Be(-3900m);
        r.Summary.ScenarioMinBalance.MonthIndex.Should().Be(1);
    }

    [Fact]
    public void Summary_TotalsSeparateTheHorizonFromTheFullImpact()
    {
        var r = Build(impacts:
        [
            new ImpactDefinition("x", "Carro", ImpactType.Expense, ImpactMode.Installment,
                new YearMonth(2026, 12), 1200m, ImpactAmountKind.Total, 12, null)
        ]);

        // 100 por parcela; o horizonte (out, nov, dez) só alcança a primeira, em dez/26.
        r.Summary.TotalImpactInHorizon.Should().Be(-100m);
        r.Summary.TotalImpactFull.Should().Be(-1200m);
        r.Warnings.Should().ContainSingle().Which.Code.Should().Be(ProjectionWarningCode.AfterWindow);
    }

    [Fact]
    public void Impacts_ComeBackAlignedToTheHorizonWithSignedAmounts()
    {
        var r = Build(impacts: [Impact(ImpactMode.Single, "2026-12", 70m, ImpactType.Income)]);

        r.Impacts.Single().Monthly.Should().Equal(0m, 0m, 70m);
        r.Scenario[2].SimulatedIncome.Should().Be(70m);
    }

    [Fact]
    public void Horizon_OfOneMonth_ProducesOneBaselineAndOneScenarioMonth()
    {
        var r = ProjectionBuilder.Build(Reference, Today, 1, Accounts(1000m), Rows(), []);

        r.Baseline.Should().ContainSingle();
        r.Scenario.Should().ContainSingle();
        r.Summary.EndBalanceBaseline.Should().Be(-450m);
    }
}
