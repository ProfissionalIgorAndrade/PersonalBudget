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
        Guid categoryId,
        TransactionType type,
        decimal amount,
        TransactionStatus status)
    {
        var account = _accountService.GetById(accountId);

        if (account is null)
            throw new Exception("Account not found.");

        var moneyAmount = new Money(amount);

        var transaction = new Transaction(
            accountId,
            categoryId,
            moneyAmount,
            type,
            status,
            DateTime.UtcNow
        );

        TransactionApplier.Apply(account, transaction);

        _transactions.Add(transaction);
        return transaction;
    }

    public IEnumerable<Transaction> GetAll()
        => _transactions;
}
