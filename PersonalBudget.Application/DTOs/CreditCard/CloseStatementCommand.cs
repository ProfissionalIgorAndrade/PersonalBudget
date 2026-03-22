namespace PersonalBudget.Application.DTOs.CreditCard;

public record CloseStatementCommand(Guid HouseholdId, Guid CreditCardId, Guid StatementId);
