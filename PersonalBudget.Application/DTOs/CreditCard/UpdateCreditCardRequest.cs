public record UpdateCreditCardRequest(
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    string? Color = null,
    /// <summary>Nova conta base. Null mantém a atual.</summary>
    Guid? AccountId = null,
    /// <summary>Perfil de membro a quem o cartão pertence. Null mantém o atual.</summary>
    Guid? MemberId = null
);
