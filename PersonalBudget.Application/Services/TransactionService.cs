using PersonalBudget.Application.Interfaces;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionQueryRepository _transactionQueryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IReadOnlyDictionary<PaymentMethod, ITransactionCreationStrategy> _creationStrategies;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ITransactionQueryRepository transactionQueryRepository,
        IAccountRepository accountRepository,
        IEnumerable<ITransactionCreationStrategy> creationStrategies)
    {
        _transactionRepository = transactionRepository;
        _transactionQueryRepository = transactionQueryRepository;
        _accountRepository = accountRepository;
        _creationStrategies = creationStrategies.ToDictionary(s => s.PaymentMethod);
    }

    public Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
        if (!_creationStrategies.TryGetValue(command.PaymentMethod, out var strategy))
            throw new DomainException($"Unsupported payment method: {command.PaymentMethod}");

        // Aqui é onde o Strategy é aplicado:
        // PaymentMethod -> estratégia -> CreateAsync da estratégia.
        return strategy.CreateAsync(command);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query)
    {
        var transactions = await _transactionQueryRepository.GetByUserAndMonthAsync(query.UserId, query.Month, query.Year);
        return MapPaymentMethodWhenTransfer(transactions);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query)
    {
        var transactions = await _transactionQueryRepository.GetByUserAsync(query.UserId);
        return MapPaymentMethodWhenTransfer(transactions);
    }

    public async Task UpdateStatusAsync(UpdateTransactionStatusCommand command)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId);

        if (transaction is null || transaction.UserId != command.UserId)
            throw new DomainException("Transaction not found.");

        var previousStatus = transaction.Status;

        if (command.Status == TransactionStatus.Pending && previousStatus == TransactionStatus.Completed)
        {
            var account = await _accountRepository.GetByIdAsync(transaction.AccountId);
            if (account is null)
                throw new DomainException("Account not found.");
            TransactionApplier.Revert(account, transaction);
            transaction.SetStatus(command.Status);
            await _accountRepository.UpdateAsync(account);
        }
        else
        {
            transaction.SetStatus(command.Status);

            if (command.Status == TransactionStatus.Completed && previousStatus != TransactionStatus.Completed)
            {
                var account = await _accountRepository.GetByIdAsync(transaction.AccountId);
                if (account is null)
                    throw new DomainException("Account not found.");
                TransactionApplier.Apply(account, transaction);
                await _accountRepository.UpdateAsync(account);
            }
        }

        await _transactionRepository.UpdateAsync(transaction);
    }

    private static IEnumerable<GetAllTransactionByUserResponse> MapPaymentMethodWhenTransfer(IEnumerable<GetAllTransactionByUserResponse> transactions)
    {
        return transactions.Select(t => t.TransferId is not null
            ? t with { PaymentMethod = "Transfer" }
            : t);
    }
}