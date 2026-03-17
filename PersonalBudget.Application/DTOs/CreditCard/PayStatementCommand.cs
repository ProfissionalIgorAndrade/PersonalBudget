namespace PersonalBudget.Application.DTOs.CreditCard;

public record PayStatementCommand(Guid UserId, Guid CreditCardId, Guid StatementId);
