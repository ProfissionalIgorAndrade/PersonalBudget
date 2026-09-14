/// <summary>
/// Aplica o efeito de uma transação concluída no saldo da conta, no momento
/// da criação.
///
/// Revert foi removido junto com a alteração de status: era o caminho de
/// volta de Completed para Pending, e esse caminho não existe mais.
/// </summary>
public static class TransactionApplier
{
    public static void Apply(Account account, Transaction transaction)
    {
        if (transaction.Status != TransactionStatus.Completed)
            throw new DomainException("Apenas transações concluídas podem ser aplicadas.");

        if (transaction.Type == TransactionType.Income)
        {
            account.Credit(transaction.Amount);
            return;
        }

        if (transaction.Type == TransactionType.Expense)
        {
            account.Debit(transaction.Amount);
            return;
        }

        throw new DomainException("Tipo de transação não suportado.");
    }
}
