/// <summary>
/// Regra única do rótulo de exibição de uma conta, usada na listagem e no resumo.
///
/// Fica na Application (e não no domínio) porque depende do nome do membro,
/// que vem de outro agregado, e porque é formatação para o cliente.
/// </summary>
public static class AccountDisplayName
{
    public const string SavingsBoxFallback = "Caixinha";

    /// <summary>
    /// Caixinha: <c>Name ?? "Caixinha"</c>.
    /// Conta corrente: <c>(Name ?? Banco)</c>, seguido de <c>" - {membro}"</c> quando há membro.
    /// </summary>
    public static string Build(Account account, string? memberName)
    {
        if (account.Kind == AccountKind.Savings)
            return account.Name ?? SavingsBoxFallback;

        var label = account.Name ?? BankLabel.Of(account.Bank);
        return memberName is null ? label : $"{label} - {memberName}";
    }
}
