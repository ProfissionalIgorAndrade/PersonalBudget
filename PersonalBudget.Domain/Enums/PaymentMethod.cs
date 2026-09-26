public enum PaymentMethod
{
    Account = 1,       // Debit, Pix
    CreditCard = 2,    // Credit card
    Cash = 3,
    Transfer = 4,

    /// <summary>
    /// Movimento de caixinha: depósito ou resgate.
    ///
    /// Existe para que o movimento seja gravado com data, valor e sentido —
    /// o que dá histórico, gráfico de evolução e extrato — sem entrar nos
    /// totais de receita e despesa do lar. Guardar não é gastar, e resgatar
    /// não é receber.
    ///
    /// Segue o mesmo caminho de Transfer, que já é excluído dos agregados.
    /// </summary>
    Savings = 5
}
