using System.Globalization;

public class TransactionService : ITransactionService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] DateFormatsBr = { "dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss" };
    private static readonly string[] DateFormatsIso = { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff", "yyyy-MM-ddTHH:mm:ss.ffffff" };

    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionQueryRepository _transactionQueryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICreditCardRepository _creditCardRepository;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ITransactionQueryRepository transactionQueryRepository,
        IAccountRepository accountRepository,
        ICreditCardRepository creditCardRepository)
    {
        _transactionRepository = transactionRepository;
        _transactionQueryRepository = transactionQueryRepository;
        _accountRepository = accountRepository;
        _creditCardRepository = creditCardRepository;
    }

    public async Task<Guid> CreateAsync(CreateTransactionCommand command)
    {

        if (command.PaymentMethod == PaymentMethod.Transfer)
        {
            return await CreateTransferAsync(command);
        }

        var accountId = await ResolveAccountIdAsync(command);

        var account = await _accountRepository.GetByIdAsync(accountId);

        if (account is null || account.UserId != command.UserId)
            throw new DomainException("Account not found.");

        var date = ParseDateBr(command.Date);

        var transaction = Transaction.Create(
            command.UserId,
            accountId,
            new Money(command.Amount),
            command.Type.Value,
            command.PaymentMethod,
            date,
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

    public async Task<Guid> CreateTransferAsync(CreateTransactionCommand command)
    {
        if (command.FromAccountId == command.ToAccountId)
            throw new DomainException("Origin and destination accounts must be different.");

        var fromAccount = await _accountRepository.GetByIdAsync(command.FromAccountId.Value);
        var toAccount = await _accountRepository.GetByIdAsync(command.ToAccountId.Value);

        if (fromAccount is null || fromAccount.UserId != command.UserId)
            throw new DomainException("Origin account not found.");

        if (toAccount is null || toAccount.UserId != command.UserId)
            throw new DomainException("Destination account not found.");

        var date = ParseDateBr(command.Date);
        var transferId = Guid.NewGuid();

        var outTx = Transaction.Create(
            command.UserId,
            command.FromAccountId.Value,
            new Money(command.Amount),
            TransactionType.Expense,
            PaymentMethod.Account,
            date,
            command.Description,
            categoryId: null,
            creditCardId: null,
            transferId);

        var inTx = Transaction.Create(
            command.UserId,
            command.ToAccountId.Value,
            new Money(command.Amount),
            TransactionType.Income,
            PaymentMethod.Account,
            date,
            command.Description,
            categoryId: null,
            creditCardId: null,
            transferId);

        outTx.Complete();
        inTx.Complete();
        TransactionApplier.Apply(fromAccount, outTx);
        TransactionApplier.Apply(toAccount, inTx);

        await _transactionRepository.AddAsync(outTx);
        await _transactionRepository.AddAsync(inTx);
        await _accountRepository.UpdateAsync(fromAccount);
        await _accountRepository.UpdateAsync(toAccount);

        return transferId;
    }

    /// <summary>
    /// Parse da data aceitando formato Brasil (dd/MM/yyyy) ou ISO (ex: 2026-03-02T12:48:00.000 = 02/03/2026 12:48).
    /// </summary>
    private static DateTime ParseDateBr(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            throw new DomainException("Data é obrigatória.");

        var trimmed = dateString.Trim();

        if (DateTime.TryParseExact(trimmed, DateFormatsBr, PtBr, DateTimeStyles.None, out var dateBr))
            return dateBr;

        if (DateTime.TryParseExact(trimmed, DateFormatsIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateIso))
            return dateIso;

        throw new DomainException("Data inválida. Use dd/MM/yyyy (ex: 02/03/2026) ou ISO (ex: 2026-03-02T12:48:00).");
    }

    private async Task<Guid> ResolveAccountIdAsync(CreateTransactionCommand command)
    {
        if (command.AccountId is { } accountId)
            return accountId;

        if (command.CreditCardId is { } creditCardId)
        {
            var creditCard = await _creditCardRepository.GetByIdAsync(creditCardId);
            if (creditCard is null || creditCard.UserId != command.UserId)
                throw new DomainException("Credit card not found.");
            return creditCard.AccountId;
        }

        throw new DomainException("Account or credit card must be provided.");
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

    public async Task<IEnumerable<Transaction>> GetByAccountAsync(GetTransactionsByAccountQuery query)
    {
        var account = await _accountRepository.GetByIdAsync(query.AccountId);

        if (account is null || account.UserId != query.UserId)
            throw new DomainException("Account not found.");

        return await _transactionRepository.GetByAccountAsync(query.AccountId);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query)
    {
        var transactions = await _transactionQueryRepository.GetByUserAsync(query.UserId);
        return MapPaymentMethodWhenTransfer(transactions);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query)
    {
        var transactions = await _transactionQueryRepository.GetByUserAndMonthAsync(query.UserId, query.Month, query.Year);
        return MapPaymentMethodWhenTransfer(transactions);
    }

    private static IEnumerable<GetAllTransactionByUserResponse> MapPaymentMethodWhenTransfer(IEnumerable<GetAllTransactionByUserResponse> transactions)
    {
        return transactions.Select(t => t.TransferId is not null
            ? t with { PaymentMethod = "Transfer" }
            : t);
    }
}
