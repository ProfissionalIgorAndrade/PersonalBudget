public record CreateCreditCardRequest(
    Guid AccountId,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    string? Color = null,
    /// <summary>Perfil de membro a quem o cartão pertence.</summary>
    Guid? MemberId = null
);
