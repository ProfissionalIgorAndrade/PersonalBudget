public enum AccountKind
{
    /// <summary>Conta corrente comum.</summary>
    Checking = 1,

    /// <summary>
    /// Caixinha: dinheiro guardado, vinculado a uma conta corrente.
    ///
    /// Não é uma entidade nova de propósito. Guardar dinheiro é mover saldo
    /// entre duas contas, que é exatamente o que uma transferência já faz — e
    /// transferência já fica fora dos totais de receita e despesa, que é o
    /// comportamento certo: guardar não é gastar.
    /// </summary>
    Savings = 2
}
