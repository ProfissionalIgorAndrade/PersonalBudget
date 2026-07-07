namespace PersonalBudget.Application.DTOs.Transaction;

public record DeleteRecurringTransactionRequest(
    RecurrenceDeleteMode RecurrenceDeleteMode);
