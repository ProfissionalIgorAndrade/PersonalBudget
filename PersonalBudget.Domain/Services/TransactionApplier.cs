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

    /// <summary>
    /// Reverte o efeito de uma transação completed na conta (para voltar ao status Pending).
    /// </summary>
    public static void Revert(Account account, Transaction transaction)
    {
        if (transaction.Status != TransactionStatus.Completed)
            throw new DomainException("Apenas transações concluídas podem ser revertidas.");

        if (transaction.Type == TransactionType.Income)
        {
            account.Debit(transaction.Amount);
            return;
        }

        if (transaction.Type == TransactionType.Expense)
        {
            account.Credit(transaction.Amount);
            return;
        }

        throw new DomainException("Tipo de transação não suportado.");
    }
}
