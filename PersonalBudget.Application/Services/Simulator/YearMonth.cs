using System.Globalization;

namespace PersonalBudget.Application.Services.Simulator;

/// <summary>Mês de calendário (sem dia), com aritmética de meses e rótulo pt-BR.</summary>
public readonly record struct YearMonth(int Year, int Month) : IComparable<YearMonth>
{
    private static readonly string[] Abbreviations =
        ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    /// <summary>Número de meses desde o ano 0; serve para comparar e subtrair meses.</summary>
    public int Ordinal => Year * 12 + (Month - 1);

    public static YearMonth FromOrdinal(int ordinal) => new(ordinal / 12, ordinal % 12 + 1);

    public static YearMonth FromDate(DateTime date) => new(date.Year, date.Month);

    public YearMonth AddMonths(int months) => FromOrdinal(Ordinal + months);

    /// <summary>Quantos meses faltam de this até other (negativo quando other é anterior).</summary>
    public int MonthsUntil(YearMonth other) => other.Ordinal - Ordinal;

    /// <summary>Rótulo curto em pt-BR, ex.: "out/26".</summary>
    public string Label => $"{Abbreviations[Month - 1]}/{Year % 100:D2}";

    /// <summary>Formato de troca da API: "yyyy-MM".</summary>
    public override string ToString() => $"{Year:D4}-{Month:D2}";

    public int CompareTo(YearMonth other) => Ordinal.CompareTo(other.Ordinal);

    public static bool operator <(YearMonth a, YearMonth b) => a.Ordinal < b.Ordinal;
    public static bool operator >(YearMonth a, YearMonth b) => a.Ordinal > b.Ordinal;
    public static bool operator <=(YearMonth a, YearMonth b) => a.Ordinal <= b.Ordinal;
    public static bool operator >=(YearMonth a, YearMonth b) => a.Ordinal >= b.Ordinal;

    /// <summary>Aceita exatamente "yyyy-MM".</summary>
    public static bool TryParse(string? raw, out YearMonth value)
    {
        if (raw is not null
            && DateTime.TryParseExact(raw, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            value = FromDate(d);
            return true;
        }

        value = default;
        return false;
    }
}
