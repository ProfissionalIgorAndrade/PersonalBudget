namespace PersonalBudget.Application.DTOs.FinancialCalendar;

/// <summary>
/// Um dia no calendário financeiro.
/// Inclui os lançamentos do dia (Pending e Completed, sem Cancelled)
/// e as faturas de cartão cujo vencimento cai neste dia.
/// ProjectedBalance é preenchido somente para hoje e dias futuros.
/// </summary>
public record FinancialCalendarDayDto(
    DateTime Date,
    IReadOnlyList<GetAllTransactionByUserResponse> Transactions,
    IReadOnlyList<FinancialCalendarStatementDto> Statements,
    decimal? ProjectedBalance);
