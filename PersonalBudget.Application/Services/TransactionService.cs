namespace PersonalBudget.Application.Services;

public class TransactionService
{
    private readonly AccountService _accountService;
    private readonly List<Transaction> _transactions = new();

    public TransactionService(AccountService accountService)
    {
        _accountService = accountService;
    }

    public Transaction Create(
        Guid accountId,
        TransactionType type,
        decimal amount,
        TransactionStatus status)
    {
        var account = _accountService.GetById(accountId);

        if (account is null)
            throw new Exception("Account not found.");

        var transaction = new Transaction(
            accountId,
            amount,
            DateTime.UtcNow,
            type,
            status
        );

        // REGRA DE NEGÓCIO CENTRAL
        if (transaction.IsCompleted())
        {
            if (type == TransactionType.Income)
                account.Credit(amount);

            if (type == TransactionType.Expense)
                account.Debit(amount);
        }

        _transactions.Add(transaction);
        return transaction;
    }

    public IEnumerable<Transaction> GetAll()
        => _transactions;
}
