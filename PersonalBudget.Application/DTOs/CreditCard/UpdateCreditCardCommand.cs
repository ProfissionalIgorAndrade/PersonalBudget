public record UpdateCreditCardCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid CreditCardId,
    string Name,
    decimal Limit,
    int DueDay,
    string? Color = null,
    /// <summary>Perfil de membro a quem o cartão pertence. Null mantém o atual.</summary>
    Guid? MemberId = null
);
