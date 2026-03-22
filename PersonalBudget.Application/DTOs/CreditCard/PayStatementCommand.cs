namespace PersonalBudget.Application.DTOs.CreditCard;

public record PayStatementCommand(Guid HouseholdId, Guid CreditCardId, Guid StatementId);
