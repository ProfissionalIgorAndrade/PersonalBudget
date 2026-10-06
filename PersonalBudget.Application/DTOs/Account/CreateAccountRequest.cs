public record CreateAccountRequest(
    Bank Bank,
    Guid MemberId,
    /// <summary>Apelido opcional, para diferenciar contas do mesmo banco.</summary>
    string? Name = null
);
