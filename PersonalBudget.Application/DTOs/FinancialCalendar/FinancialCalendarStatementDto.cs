namespace PersonalBudget.Application.DTOs.FinancialCalendar;

/// <summary>Fatura de cartão que vence em um dia do calendário.</summary>
public record FinancialCalendarStatementDto(
    Guid StatementId,
    Guid CreditCardId,
    string CreditCardName,
    DateTime DueDate,
    decimal TotalAmount,
    string Status);
