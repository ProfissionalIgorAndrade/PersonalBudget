using PersonalBudget.Application.DTOs.FinancialCalendar;

namespace PersonalBudget.Application.Interfaces;

public interface IFinancialCalendarService
{
    /// <summary>
    /// Retorna o calendário financeiro do lar para o período [from, to].
    /// Inclui lançamentos agrupados por dia, faturas de cartão com vencimento no período
    /// e saldo projetado acumulativo para hoje e dias futuros.
    /// </summary>
    Task<FinancialCalendarResponse> GetCalendarAsync(Guid householdId, DateTime from, DateTime to);
}
