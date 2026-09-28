using PersonalBudget.Application.DTOs.Simulator;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Application.Services;

public class SimulatorService : ISimulatorService
{
    private readonly ITransactionQueryRepository _queryRepository;
    private readonly IAccountRepository _accountRepository;

    public SimulatorService(
        ITransactionQueryRepository queryRepository,
        IAccountRepository accountRepository)
    {
        _queryRepository = queryRepository;
        _accountRepository = accountRepository;
    }

    public async Task<ScenarioComparisonResponse> CalculateAsync(SimulateScenarioCommand command)
    {
        var now = DateTime.UtcNow;

        // 1. Baseline: use the current month's actual data so numbers match what the user sees on the dashboard
        var currentSummary    = await _queryRepository.GetDashboardSummaryAsync(command.HouseholdId, now.Month, now.Year);

        decimal avgIncome         = currentSummary.TotalIncome;
        decimal avgTotalExpense   = currentSummary.TotalExpense;
        decimal avgCardExpense    = currentSummary.CreditCardInvoice;
        decimal avgAccountExpense = Math.Max(0, avgTotalExpense - avgCardExpense);

        // 2. Build month-by-month projection starting from zero so accumulated balance matches dashboard saldo
        int n = Math.Clamp(command.Months, 1, 24);
        var months = new List<ScenarioMonthDto>(n);

        decimal baseBalance     = 0m;
        decimal scenarioBalance = 0m;

        for (int i = 0; i < n; i++)
        {
            // Start from the current month (i=0 = now) so impacts dated this month are included
            var projectedDate = now.AddMonths(i);
            int year  = projectedDate.Year;
            int month = projectedDate.Month;

            // Base projection: for the current month use actual data, future months use current month as baseline
            decimal monthIncome  = avgIncome;
            decimal monthAccount = avgAccountExpense;
            decimal monthCard    = avgCardExpense;

            baseBalance += monthIncome - monthAccount - monthCard;

            // Scenario: apply simulated impacts for this month
            decimal simulatedIncome  = 0m;
            decimal simulatedExpense = 0m;

            foreach (var impact in command.Impacts)
            {
                if (!TryParseDate(impact.StartDate, out var startDate)) continue;
                if (!AppliesToMonth(impact, year, month, startDate, n - i)) continue;

                if (string.Equals(impact.Type, "Income", StringComparison.OrdinalIgnoreCase))
                    simulatedIncome += impact.Amount;
                else
                    simulatedExpense += impact.Amount;
            }

            scenarioBalance += monthIncome + simulatedIncome - monthAccount - monthCard - simulatedExpense;

            months.Add(new ScenarioMonthDto(
                Year:                year,
                Month:               month,
                Label:               FormatLabel(year, month),
                BaseIncome:          avgIncome,
                BaseAccountExpense:  avgAccountExpense,
                BaseCardExpense:     avgCardExpense,
                BaseBalance:         baseBalance,
                SimulatedIncome:     simulatedIncome,
                SimulatedExpense:    simulatedExpense,
                ScenarioBalance:     scenarioBalance,
                BalanceDelta:        scenarioBalance - baseBalance
            ));
        }

        // 4. Summary
        int? firstNegative = null;
        for (int i = 0; i < months.Count; i++)
        {
            if (months[i].ScenarioBalance < 0) { firstNegative = i; break; }
        }

        decimal baseEnd     = months.Count > 0 ? months[^1].BaseBalance     : 0m;
        decimal scenarioEnd = months.Count > 0 ? months[^1].ScenarioBalance : 0m;

        // TotalSimulatedImpact: signed sum of all impacts across the window (same index as projection loop)
        decimal totalSimulated = 0m;
        for (int i = 0; i < n; i++)
        {
            var projectedDate = now.AddMonths(i);
            foreach (var impact in command.Impacts)
            {
                if (!TryParseDate(impact.StartDate, out var startDate)) continue;
                if (!AppliesToMonth(impact, projectedDate.Year, projectedDate.Month, startDate, n - i)) continue;

                decimal signed = string.Equals(impact.Type, "Income", StringComparison.OrdinalIgnoreCase)
                    ? impact.Amount
                    : -impact.Amount;
                totalSimulated += signed;
            }
        }

        var summary = new ScenarioSummaryDto(
            HorizonTotalIncome:          avgIncome          * n,
            HorizonTotalAccountExpense:  avgAccountExpense  * n,
            HorizonTotalCardExpense:     avgCardExpense      * n,
            TotalSimulatedImpact:        totalSimulated,
            BaseEndBalance:              baseEnd,
            ScenarioEndBalance:          scenarioEnd,
            EndBalanceDelta:             scenarioEnd - baseEnd,
            ScenarioGoesNegative:        firstNegative.HasValue,
            FirstNegativeMonthIndex:     firstNegative
        );

        return new ScenarioComparisonResponse(
            ScenarioName:              command.ScenarioName ?? "Cenário",
            BaseMonthlyIncome:         avgIncome,
            BaseMonthlyAccountExpense: avgAccountExpense,
            BaseMonthlyCardExpense:    avgCardExpense,
            CurrentBalance:            0m,
            Months:                    months,
            Summary:                   summary
        );
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private static bool AppliesToMonth(ScenarioImpactDto impact, int year, int month, DateTime startDate, int remainingMonths)
    {
        var current = new DateTime(year, month, 1);
        var start   = new DateTime(startDate.Year, startDate.Month, 1);

        if (current < start) return false;

        var monthsFromStart = (current.Year - start.Year) * 12 + (current.Month - start.Month);

        return impact.Mode switch
        {
            "Single"      => monthsFromStart == 0,
            "Installment" => monthsFromStart < impact.InstallmentCount,
            "Monthly"     => true,   // applies from start through end of window
            _             => false,
        };
    }

    private static bool TryParseDate(string? raw, out DateTime date)
    {
        if (raw is not null && DateTime.TryParseExact(raw, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out date))
            return true;

        date = default;
        return false;
    }

    private static string FormatLabel(int year, int month)
    {
        var abbr = new[] { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun",
                           "Jul", "Ago", "Set", "Out", "Nov", "Dez" };
        return $"{abbr[month - 1]}/{year % 100:D2}";
    }
}
