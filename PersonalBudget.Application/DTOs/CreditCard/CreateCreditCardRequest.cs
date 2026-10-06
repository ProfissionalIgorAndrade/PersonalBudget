public record CreateCreditCardRequest(
    string Name,
    decimal Limit,
    int DueDay,
    string? Color = null,
    /// <summary>Perfil de membro a quem o cartão pertence.</summary>
    Guid? MemberId = null
);
