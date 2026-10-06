using PersonalBudget.Application.DTOs.Simulator;

namespace PersonalBudget.Application.Services.Simulator;

/// <summary>
/// Monta baseline, saldo corrente, cenário e resumo a partir de totais já agregados.
/// Matemática pura, sem I/O.
/// </summary>
public static class ProjectionBuilder
{
    /// <summary>Quantos meses completos anteriores ao mês de referência entram nas médias.</summary>
    public const int LookbackMonths = 3;

    public static ProjectionResponse Build(
        YearMonth reference,
        DateTime today,
        int horizonMonths,
        IReadOnlyList<OpeningAccountDto> openingAccounts,
        IReadOnlyList<ProjectionFlowRow> rows,
        IReadOnlyList<ImpactDefinition> impacts)
    {
        var openingBalance = openingAccounts.Sum(a => a.Balance);

        var (avgIncome, avgVariable, monthsWithData) = ComputeAverages(rows, reference);

        // ─── baseline ───
        var baseline = new List<BaselineMonthDto>(horizonMonths);
        var running = openingBalance;

        for (var i = 0; i < horizonMonths; i++)
        {
            var month = reference.AddMonths(i);
            var bucket = rows.Where(r => r.Year == month.Year && r.Month == month.Month).ToList();

            // Mês de referência: o que é de conta e tem data até hoje já está no saldo de partida,
            // então não entra de novo no fluxo. Conta para "já lançado" (reduz as estimativas).
            decimal Counted(Func<ProjectionFlowRow, bool> filter) =>
                bucket.Where(filter).Sum(r => r.Total - (i == 0 && !r.IsCard ? r.PostedByTodayTotal : 0m));

            decimal Posted(Func<ProjectionFlowRow, bool> filter) =>
                bucket.Where(filter).Sum(r => r.Total);

            bool IsIncome(ProjectionFlowRow r) => r.Type == TransactionType.Income;
            bool IsCommitted(ProjectionFlowRow r) => r.Type == TransactionType.Expense
                && r.Frequency is TransactionFrequency.Fixed or TransactionFrequency.Installments;
            bool IsVariable(ProjectionFlowRow r) => r.Type == TransactionType.Expense
                && r.Frequency == TransactionFrequency.Variable;

            var income = Counted(IsIncome) + Math.Max(0m, (avgIncome ?? 0m) - Posted(IsIncome));
            var committed = Counted(IsCommitted);
            var variable = Counted(IsVariable) + Math.Max(0m, (avgVariable ?? 0m) - Posted(IsVariable));

            var result = income - committed - variable;
            running += result;

            baseline.Add(new BaselineMonthDto(
                month.Year, month.Month, month.Label, income, committed, variable, result, running));
        }

        // ─── impacts ───
        var scheduled = impacts.Select(i => ImpactSchedule.Build(i, reference, horizonMonths)).ToList();
        var impactDtos = scheduled.Select(s => s.Projection).ToList();
        var warnings = scheduled.SelectMany(s => s.Warnings).ToList();

        // ─── scenario ───
        var scenario = new List<ProjectionScenarioMonthDto>(horizonMonths);
        var scenarioRunning = openingBalance;

        for (var i = 0; i < horizonMonths; i++)
        {
            var simulatedIncome = impactDtos.Sum(d => Math.Max(0m, d.Monthly[i]));
            var simulatedExpense = impactDtos.Sum(d => Math.Max(0m, -d.Monthly[i]));

            var result = baseline[i].Result + simulatedIncome - simulatedExpense;
            scenarioRunning += result;

            scenario.Add(new ProjectionScenarioMonthDto(
                baseline[i].Year, baseline[i].Month, baseline[i].Label,
                simulatedIncome, simulatedExpense, result, scenarioRunning,
                scenarioRunning - baseline[i].Balance));
        }

        var summary = new ProjectionSummaryDto(
            BaselineMinBalance: MinBalance(baseline.Select(b => b.Balance).ToList()),
            ScenarioMinBalance: MinBalance(scenario.Select(s => s.Balance).ToList()),
            FirstNegativeMonthIndexBaseline: FirstNegative(baseline.Select(b => b.Balance).ToList()),
            FirstNegativeMonthIndexScenario: FirstNegative(scenario.Select(s => s.Balance).ToList()),
            EndBalanceBaseline: baseline[^1].Balance,
            EndBalanceScenario: scenario[^1].Balance,
            TotalImpactInHorizon: impactDtos.Sum(d => d.TotalInHorizon),
            TotalImpactFull: impactDtos.Sum(d => d.TotalFull));

        var assumptions = new ProjectionAssumptionsDto(
            LookbackMonths, monthsWithData, avgIncome, avgVariable, BuildNotes(avgIncome, avgVariable, monthsWithData));

        return new ProjectionResponse(
            ReferenceMonth: reference.ToString(),
            OpeningBalance: new OpeningBalanceDto(
                openingBalance, today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                openingAccounts, ExcludesSavings: true),
            Assumptions: assumptions,
            Baseline: baseline,
            Impacts: impactDtos,
            Scenario: scenario,
            Summary: summary,
            Warnings: warnings);
    }

    /// <summary>
    /// Média de receita e de despesa variável dos <see cref="LookbackMonths"/> meses completos
    /// anteriores à referência. Mês sem nenhum lançamento é ignorado; sem nenhum mês com dados
    /// a média é nula (a estimativa vira 0).
    /// </summary>
    public static (decimal? AverageIncome, decimal? AverageVariable, int MonthsWithData) ComputeAverages(
        IReadOnlyList<ProjectionFlowRow> rows, YearMonth reference)
    {
        var incomeSum = 0m;
        var variableSum = 0m;
        var monthsWithData = 0;

        for (var k = 1; k <= LookbackMonths; k++)
        {
            var month = reference.AddMonths(-k);
            var bucket = rows.Where(r => r.Year == month.Year && r.Month == month.Month).ToList();
            if (bucket.Count == 0)
                continue;

            monthsWithData++;
            incomeSum += bucket.Where(r => r.Type == TransactionType.Income).Sum(r => r.Total);
            variableSum += bucket
                .Where(r => r.Type == TransactionType.Expense && r.Frequency == TransactionFrequency.Variable)
                .Sum(r => r.Total);
        }

        if (monthsWithData == 0)
            return (null, null, 0);

        return (
            Math.Round(incomeSum / monthsWithData, 2, MidpointRounding.AwayFromZero),
            Math.Round(variableSum / monthsWithData, 2, MidpointRounding.AwayFromZero),
            monthsWithData);
    }

    private static MinBalanceDto MinBalance(IReadOnlyList<decimal> balances)
    {
        var index = 0;
        for (var i = 1; i < balances.Count; i++)
            if (balances[i] < balances[index])
                index = i;

        return new MinBalanceDto(balances[index], index);
    }

    private static int? FirstNegative(IReadOnlyList<decimal> balances)
    {
        for (var i = 0; i < balances.Count; i++)
            if (balances[i] < 0)
                return i;

        return null;
    }

    private static IReadOnlyList<string> BuildNotes(decimal? avgIncome, decimal? avgVariable, int monthsWithData)
    {
        var notes = new List<string>
        {
            "Saldo de partida: soma do saldo de hoje das contas correntes ativas (lançamentos com data até hoje). Caixinhas ficam de fora.",
            "Transferências e movimentos de caixinha não entram em receitas nem em despesas.",
            "Compras de cartão não mexem no saldo da conta: entram no mês da fatura (mês do vencimento).",
            "Compromissos: despesas fixas e parceladas já lançadas em cada mês, incluindo a fatura do cartão.",
            monthsWithData == 0
                ? $"Sem lançamentos nos {LookbackMonths} meses anteriores: as estimativas de receita e de gasto variável são 0."
                : $"Médias de receita e de gasto variável: {monthsWithData} de {LookbackMonths} meses completos anteriores ao mês atual (meses sem lançamentos são ignorados).",
            "Receita do mês: o que já está lançado mais o que faltar para chegar à média de receita (nunca menos que o lançado).",
            "Gasto variável do mês: o que já está lançado mais o que faltar para chegar à média de gasto variável (nunca menos que o lançado).",
            "Mês atual: lançamentos de conta com data até hoje já estão no saldo de partida e não são contados de novo; entram a fatura inteira do cartão do mês, lançamentos de conta com data depois de hoje e o restante estimado.",
            "Despesas fixas futuras só existem até onde foram lançadas; depois disso o baseline não as repete.",
        };

        return notes;
    }
}
