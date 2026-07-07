using PersonalBudget.Application.DTOs.Transaction;

public record DeleteRecurringTransactionCommand(
    Guid HouseholdId,
    Guid TransactionId,
    RecurrenceDeleteMode RecurrenceDeleteMode);
