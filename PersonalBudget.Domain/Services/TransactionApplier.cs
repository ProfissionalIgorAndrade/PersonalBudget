public static class TransactionApplier
{
    public static void Apply(Account account, Transaction transaction)
    {
        if (!transaction.IsCompleted())
            return;

        if (transaction.Type == TransactionType.Income)
            account.Credit(transaction.Amount);

        if (transaction.Type == TransactionType.Expense)
            account.Debit(transaction.Amount);
    }
}