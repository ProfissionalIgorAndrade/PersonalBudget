public record DeleteCreditCardCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid CreditCardId
);
