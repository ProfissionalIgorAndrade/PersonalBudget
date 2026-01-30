public record DeleteCreditCardCommand(
    Guid UserId,
    Guid CreditCardId
);
