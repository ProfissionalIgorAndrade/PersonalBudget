namespace PersonalBudget.Application.DTOs.FinancialCalendar;

/// <summary>
/// Resposta do Calendário Financeiro para um período [From, To].
/// OpeningBalance = soma dos saldos atuais das contas correntes (Completed only).
/// Days inclui todos os dias do período, mesmo os sem movimentação.
/// </summary>
public record FinancialCalendarResponse(
    DateTime From,
    DateTime To,
    decimal OpeningBalance,
    IReadOnlyList<FinancialCalendarDayDto> Days);
