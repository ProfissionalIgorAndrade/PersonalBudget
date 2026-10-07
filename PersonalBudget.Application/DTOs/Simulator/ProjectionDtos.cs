namespace PersonalBudget.Application.DTOs.Simulator;

// ─── input ──────────────────────────────────────────────────────────────────

public enum ImpactType
{
    Income = 1,
    Expense = 2
}

public enum ImpactMode
{
    /// <summary>Acontece uma vez, no mês de início.</summary>
    Single = 1,
    /// <summary>N parcelas consecutivas a partir do mês de início.</summary>
    Installment = 2,
    /// <summary>Todo mês a partir do início, por uma duração ou até o fim do horizonte.</summary>
    Monthly = 3
}

public enum ImpactAmountKind
{
    /// <summary>O valor informado é o de cada parcela.</summary>
    PerInstallment = 1,
    /// <summary>O valor informado é o total; a parcela é total/n arredondada a 2 casas, com o resto na última.</summary>
    Total = 2
}

public record ProjectionImpactInput(
    string? Id,
    string? Description,
    ImpactType Type,
    ImpactMode Mode,
    string? StartMonth,        // yyyy-MM
    decimal Amount,
    ImpactAmountKind? AmountKind,
    int? Installments,         // Installment: 1..120
    int? Months                // Monthly: duração; null/0 = até o fim do horizonte
);

public record ProjectionCommand(
    Guid HouseholdId,
    string? Today,             // yyyy-MM-dd; nulo = data do servidor em UTC-3
    int Months,                // horizonte 1..24
    IReadOnlyList<ProjectionImpactInput>? Impacts
);

// ─── aggregated I/O rows ────────────────────────────────────────────────────

/// <summary>
/// Totais agregados no banco de um mês (mês da fatura para cartão, mês da data para o resto),
/// já sem Transfer e Savings.
/// </summary>
/// <param name="Total">Soma de todos os lançamentos do grupo.</param>
/// <param name="PostedByTodayTotal">
/// Parte de <paramref name="Total"/> com data até hoje em lançamentos fora de cartão (já está
/// no saldo de partida). Sempre 0 para cartão.
/// </param>
public record ProjectionFlowRow(
    int Year,
    int Month,
    bool IsCard,
    TransactionType Type,
    TransactionFrequency Frequency,
    decimal Total,
    decimal PostedByTodayTotal
);

// ─── output ─────────────────────────────────────────────────────────────────

public enum ProjectionWarningCode
{
    /// <summary>O impacto termina antes do mês de referência: não afeta o período.</summary>
    BeforeWindow = 1,
    /// <summary>O impacto começa antes do mês de referência: só os meses dentro da janela entram.</summary>
    Truncated = 2,
    /// <summary>O impacto continua depois do horizonte: o total completo é maior que o do horizonte.</summary>
    AfterWindow = 3
}

public record ProjectionWarningDto(string ImpactId, ProjectionWarningCode Code, string Message);

public record OpeningAccountDto(Guid Id, string Name, decimal Balance);

public record OpeningBalanceDto(
    decimal Amount,
    string AsOf,                                  // yyyy-MM-dd
    IReadOnlyList<OpeningAccountDto> Accounts,
    bool ExcludesSavings
);

public record ProjectionAssumptionsDto(
    int LookbackMonths,
    int MonthsWithData,
    decimal? AverageIncome,
    decimal? AverageVariableExpense,
    IReadOnlyList<string> Notes
);

public record BaselineMonthDto(
    int Year,
    int Month,
    string Label,
    decimal Income,
    decimal Committed,
    decimal Variable,
    decimal Result,
    decimal Balance,
    FullMonthDto FullMonth
);

/// <summary>
/// Mês de calendário inteiro (mês da fatura para cartão), sem descontar o que já está no saldo de partida.
/// Nos meses futuros é igual a <c>Income</c>, <c>Committed + Variable</c> e <c>Result</c> do baseline.
/// </summary>
public record FullMonthDto(
    decimal Income,
    decimal Expense,
    decimal Result
);

/// <param name="Monthly">Valores assinados (receita positiva, despesa negativa), alinhados ao horizonte.</param>
public record ImpactProjectionDto(
    string Id,
    string Description,
    ImpactType Type,
    ImpactMode Mode,
    IReadOnlyList<decimal> Monthly,
    decimal TotalInHorizon,
    decimal TotalFull,
    decimal? InstallmentAmount,
    decimal? LastInstallmentAmount,
    int InstallmentsInHorizon,
    int? InstallmentsTotal
);

public record ProjectionScenarioMonthDto(
    int Year,
    int Month,
    string Label,
    decimal SimulatedIncome,
    decimal SimulatedExpense,
    decimal Result,
    decimal Balance,
    decimal Delta
);

public record MinBalanceDto(decimal Amount, int MonthIndex);

public record ProjectionSummaryDto(
    MinBalanceDto BaselineMinBalance,
    MinBalanceDto ScenarioMinBalance,
    int? FirstNegativeMonthIndexBaseline,
    int? FirstNegativeMonthIndexScenario,
    decimal EndBalanceBaseline,
    decimal EndBalanceScenario,
    decimal TotalImpactInHorizon,
    decimal TotalImpactFull
);

public record ProjectionResponse(
    string ReferenceMonth,                        // yyyy-MM
    OpeningBalanceDto OpeningBalance,
    ProjectionAssumptionsDto Assumptions,
    IReadOnlyList<BaselineMonthDto> Baseline,
    IReadOnlyList<ImpactProjectionDto> Impacts,
    IReadOnlyList<ProjectionScenarioMonthDto> Scenario,
    ProjectionSummaryDto Summary,
    IReadOnlyList<ProjectionWarningDto> Warnings
);
