namespace PersonalBudget.Application.DTOs.CreditCard;

public record CloseStatementCommand(Guid CreditCardId, Guid StatementId);
