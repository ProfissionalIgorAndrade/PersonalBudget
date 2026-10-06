public record CreateCreditCardCommand(
    Guid UserId,
    Guid HouseholdId,
    string Name,
    decimal Limit,
    int DueDay,
    string? Color = null,
    /// <summary>Perfil de membro a quem o cartão pertence.</summary>
    Guid? MemberId = null
);
