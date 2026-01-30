public static class TransactionApplier
{
    public static void Apply(Account account, Transaction transaction)
    {
        if (transaction.Status != TransactionStatus.Completed)
            throw new DomainException("Only completed transactions can be applied.");

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

        throw new DomainException("Unsupported transaction type.");
    }
}
