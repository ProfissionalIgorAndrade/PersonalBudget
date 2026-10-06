/// <summary>
/// Rótulo de exibição de um banco, usado no nome de exibição das contas.
///
/// Os bancos originais mantêm o texto de sempre (inclusive "Itau" sem acento) para não
/// alterar nomes já exibidos. O nome cru do enum continua sendo o contrato de
/// <c>AccountResponse.Bank</c>.
/// </summary>
public static class BankLabel
{
    public static string Of(Bank bank) => bank switch
    {
        Bank.BancoDoBrasil => "Banco do Brasil",
        Bank.Btg => "BTG Pactual",
        Bank.C6Bank => "C6 Bank",
        Bank.Safra => "Safra",
        Bank.Sicoob => "Sicoob",
        Bank.Sicredi => "Sicredi",
        Bank.Original => "Banco Original",
        Bank.Pan => "Banco Pan",
        Bank.Neon => "Neon",
        Bank.PicPay => "PicPay",
        Bank.MercadoPago => "Mercado Pago",
        Bank.Banrisul => "Banrisul",
        Bank.Next => "Next",
        Bank.Bmg => "Banco BMG",
        Bank.Xp => "XP",
        Bank.PagBank => "PagBank",
        Bank.Bv => "Banco BV",
        Bank.Outro => "Outro",
        _ => bank.ToString()
    };
}
