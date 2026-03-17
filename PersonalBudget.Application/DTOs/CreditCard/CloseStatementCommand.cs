namespace PersonalBudget.Application.DTOs.CreditCard;

public record CloseStatementCommand(Guid UserId, Guid CreditCardId, Guid StatementId);
