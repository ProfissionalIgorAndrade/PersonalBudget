public record UpdateAccountRequest(
    Bank Bank,
    Guid? MemberId,
    /// <summary>Apelido opcional. Vazio remove o apelido da conta corrente.</summary>
    string? Name = null
);