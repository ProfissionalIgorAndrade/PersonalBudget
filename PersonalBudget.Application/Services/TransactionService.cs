public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionQueryRepository _transactionQueryRepository;
    private readonly IAccountRepository _accountRepository;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ITransactionQueryRepository transactionQueryRepository,
        IAccountRepository accountRepository)
    {
        _transactionRepository = transactionRepository;
        _transactionQueryRepository = transactionQueryRepository;
        _accountRepository = accountRepository;
    }

    public async Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
        var account = await _accountRepository.GetByIdAsync(command.AccountId);

        if (account is null || account.UserId != command.UserId)
            throw new DomainException("Account not found.");

        var transaction = Transaction.Create(
            command.UserId,
            command.AccountId,
            new Money(command.Amount),
            command.Type,
            command.PaymentMethod,
            command.Date,
            command.Description,
            command.CategoryId,
            command.CreditCardId
        );

        if (command.AutoComplete)
        {
            transaction.Complete();
            TransactionApplier.Apply(account, transaction);
            await _accountRepository.UpdateAsync(account);
        }

        await _transactionRepository.AddAsync(transaction);
        return transaction.Id;
    }

    public async Task CompleteAsync(CompleteTransactionCommand command)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId);

        if (transaction is null || transaction.UserId != command.UserId)
            throw new DomainException("Transaction not found.");

        if (transaction.Status != TransactionStatus.Pending)
            throw new DomainException("Only pending transactions can be completed.");

        var account = await _accountRepository.GetByIdAsync(transaction.AccountId);

        if (account is null)
            throw new DomainException("Account not found.");

        transaction.Complete();
        TransactionApplier.Apply(account, transaction);

        await _transactionRepository.UpdateAsync(transaction);
        await _accountRepository.UpdateAsync(account);
    }

    public async Task CancelAsync(CancelTransactionCommand command)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId);

        if (transaction is null || transaction.UserId != command.UserId)
            throw new DomainException("Transaction not found.");

        if (transaction.Status == TransactionStatus.Completed)
            throw new DomainException("Completed transactions cannot be cancelled.");

        transaction.Cancel();
        await _transactionRepository.UpdateAsync(transaction);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountAsync(GetTransactionsByAccountQuery query)
    {
        var account = await _accountRepository.GetByIdAsync(query.AccountId);

        if (account is null || account.UserId != query.UserId)
            throw new DomainException("Account not found.");

        return await _transactionRepository.GetByAccountAsync(query.AccountId);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query)
    {
        return await _transactionQueryRepository.GetByUserAsync(query.UserId);
    }
}
