public record UpdateCreditCardRequest(
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    string? Color = null,
    /// <summary>Nova conta base. Null mantém a atual.</summary>
    Guid? AccountId = null
);
