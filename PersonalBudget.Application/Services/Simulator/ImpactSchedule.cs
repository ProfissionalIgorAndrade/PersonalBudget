using PersonalBudget.Application.DTOs.Simulator;

namespace PersonalBudget.Application.Services.Simulator;

/// <summary>Impacto já validado, pronto para ser colocado na linha do tempo.</summary>
/// <param name="Installments">Só usado em Installment (1..120).</param>
/// <param name="Months">Só usado em Monthly: duração; null ou 0 = até o fim do horizonte.</param>
public sealed record ImpactDefinition(
    string Id,
    string Description,
    ImpactType Type,
    ImpactMode Mode,
    YearMonth StartMonth,
    decimal Amount,
    ImpactAmountKind AmountKind,
    int Installments,
    int? Months);

public sealed record ScheduledImpact(
    ImpactProjectionDto Projection,
    IReadOnlyList<ProjectionWarningDto> Warnings);

/// <summary>
/// Coloca um impacto na linha do tempo do horizonte. Matemática pura, sem I/O.
///
/// Regras:
/// - Single: uma ocorrência no mês de início.
/// - Installment: n ocorrências consecutivas. PerInstallment usa o valor como dado; Total usa
///   Round(total/n, 2) e deixa o resto na última, igual à criação real de parcelas de cartão.
/// - Monthly: uma ocorrência por mês, por <see cref="ImpactDefinition.Months"/> meses, ou do
///   início até o fim do horizonte quando não há duração.
/// - Ocorrências fora da janela não entram nos vetores mensais nem em TotalInHorizon, mas
///   entram em TotalFull e geram aviso.
/// </summary>
public static class ImpactSchedule
{
    public static ScheduledImpact Build(ImpactDefinition impact, YearMonth reference, int horizonMonths)
    {
        var horizonEnd = reference.AddMonths(horizonMonths - 1);
        var sign = impact.Type == ImpactType.Income ? 1m : -1m;

        var (perAmount, lastAmount) = AmountsFor(impact);

        // null = sem fim definido (Monthly sem duração): vai até o fim do horizonte.
        int? definedCount = impact.Mode switch
        {
            ImpactMode.Single => 1,
            ImpactMode.Installment => impact.Installments,
            _ => impact.Months is > 0 ? impact.Months : null
        };

        var occurrences = definedCount
            ?? Math.Max(0, impact.StartMonth.MonthsUntil(horizonEnd) + 1);

        var monthly = new decimal[horizonMonths];
        var totalInHorizon = 0m;
        var totalFull = 0m;
        var inHorizon = 0;

        for (var k = 0; k < occurrences; k++)
        {
            var amount = k == occurrences - 1 ? lastAmount : perAmount;
            totalFull += sign * amount;

            var index = reference.MonthsUntil(impact.StartMonth.AddMonths(k));
            if (index < 0 || index >= horizonMonths)
                continue;

            monthly[index] = sign * amount;
            totalInHorizon += sign * amount;
            inHorizon++;
        }

        var warnings = new List<ProjectionWarningDto>();

        if (impact.StartMonth < reference)
        {
            warnings.Add(inHorizon > 0
                ? new ProjectionWarningDto(impact.Id, ProjectionWarningCode.Truncated,
                    $"\"{impact.Description}\" começa antes de {reference.Label}: só os meses a partir de {reference.Label} entram na projeção.")
                : new ProjectionWarningDto(impact.Id, ProjectionWarningCode.BeforeWindow,
                    $"\"{impact.Description}\" termina antes de {reference.Label} e não afeta o período projetado."));
        }

        if (impact.StartMonth > horizonEnd)
        {
            warnings.Add(new ProjectionWarningDto(impact.Id, ProjectionWarningCode.AfterWindow,
                $"\"{impact.Description}\" começa depois de {horizonEnd.Label} e não entra no horizonte; só o total completo o considera."));
        }
        else if (definedCount is { } count && impact.StartMonth.AddMonths(count - 1) > horizonEnd)
        {
            warnings.Add(new ProjectionWarningDto(impact.Id, ProjectionWarningCode.AfterWindow,
                $"\"{impact.Description}\" continua depois de {horizonEnd.Label}: o total no horizonte considera só até lá e o total completo inclui todas as ocorrências."));
        }

        var isInstallment = impact.Mode == ImpactMode.Installment;

        var projection = new ImpactProjectionDto(
            Id: impact.Id,
            Description: impact.Description,
            Type: impact.Type,
            Mode: impact.Mode,
            Monthly: monthly,
            TotalInHorizon: totalInHorizon,
            TotalFull: totalFull,
            InstallmentAmount: isInstallment ? perAmount : null,
            LastInstallmentAmount: isInstallment ? lastAmount : null,
            InstallmentsInHorizon: inHorizon,
            InstallmentsTotal: definedCount);

        return new ScheduledImpact(projection, warnings);
    }

    /// <summary>Valor das ocorrências comuns e o da última (só difere em Installment com Total).</summary>
    private static (decimal PerAmount, decimal LastAmount) AmountsFor(ImpactDefinition impact)
    {
        if (impact.Mode != ImpactMode.Installment || impact.AmountKind == ImpactAmountKind.PerInstallment)
            return (impact.Amount, impact.Amount);

        var count = impact.Installments;
        var perInstallment = Math.Round(impact.Amount / count, 2);
        var last = impact.Amount - perInstallment * (count - 1);
        return (perInstallment, last);
    }
}
