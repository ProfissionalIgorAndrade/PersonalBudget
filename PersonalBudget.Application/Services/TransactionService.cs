using PersonalBudget.Application.Interfaces;

public class TransactionService : ITransactionService
{
    public const int TransactionsByMonthPageSize = 15;

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

    public async Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
        ValidateTransactionFrequency(command);

        if (!_creationStrategies.TryGetValue(command.PaymentMethod, out var strategy))
            throw new DomainException($"Método de pagamento não suportado: {command.PaymentMethod}");

        var repeatCount = command.RepeatCount ?? 1;
        if (repeatCount > 1)
        {
            if (command.PaymentMethod != PaymentMethod.Account)
                throw new DomainException(String.Format("{0} recorrente só é permitida com método de pagamento Conta. PaymentMethod: {1}", command.Type, command.PaymentMethod));
            var dueDay = command.DueDay ?? ParseDayFromDate(command.Date);
            if (dueDay is < 1 or > 31)
                throw new DomainException("Para recorrência, Data de vencimento deve ser entre 1 e 31.");
            return await CreateRecurringAsync(command with { DueDay = dueDay }, strategy, repeatCount);
        }

        return await strategy.CreateAsync(command);
    }

    private static void ValidateTransactionFrequency(CreateTransactionCommand command)
    {
        var installments = command.InstallmentCount ?? 1;
        var repeat = command.RepeatCount ?? 1;

        if (installments > 1)
        {
            if (command.Frequency != TransactionFrequency.Installments)
                throw new DomainException("Parcelas exigem TransactionFrequency.Installments.");
            if (command.PaymentMethod != PaymentMethod.CreditCard)
                throw new DomainException("Parcelamento só é permitido com cartão de crédito.");
        }
        else if (command.Frequency == TransactionFrequency.Installments)
        {
            throw new DomainException("TransactionFrequency.Installments exige InstallmentCount maior que 1.");
        }

        if (repeat > 1)
        {
            if (command.Frequency != TransactionFrequency.Fixed)
                throw new DomainException("Recorrência exige TransactionFrequency.Fixed.");
        }

        if (command.PaymentMethod == PaymentMethod.Transfer && command.Frequency != TransactionFrequency.Variable)
            throw new DomainException("Transferências devem usar TransactionFrequency.Variable.");

        if (command.PaymentMethod == PaymentMethod.CreditCard && installments <= 1 && command.Frequency != TransactionFrequency.Variable)
            throw new DomainException("Compra à vista no cartão deve usar TransactionFrequency.Variable.");

        if (!string.IsNullOrWhiteSpace(command.ExpirationDate))
        {
            if (command.Frequency != TransactionFrequency.Fixed)
                throw new DomainException("ExpirationDate só é permitido com TransactionFrequency.Fixed.");
        }
    }

    private async Task<Guid> CreateRecurringAsync(CreateTransactionCommand command, ITransactionCreationStrategy strategy, int repeatCount)
    {
        var firstDate = ParseFirstDate(command.Date, command.DueDay!.Value);
        Guid? firstId = null;

        for (var i = 0; i < repeatCount; i++)
        {
            var dueDate = firstDate.AddMonths(i);
            var day = Math.Min(command.DueDay!.Value, System.DateTime.DaysInMonth(dueDate.Year, dueDate.Month));
            var date = new System.DateTime(dueDate.Year, dueDate.Month, day);
            var dateString = date.ToString("dd/MM/yyyy");

            var singleCommand = command with
            {
                Date = dateString,
                AutoComplete = false,
                RepeatCount = null,
                DueDay = null
            };

            var id = await strategy.CreateAsync(singleCommand);
            if (firstId is null)
                firstId = id;
        }

        return firstId!.Value;
    }

    private static int ParseDayFromDate(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            throw new DomainException("Data é obrigatória para despesa/receita fixa.");
        var formats = new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss" };
        if (!System.DateTime.TryParseExact(dateString.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
            throw new DomainException("Data inválida. Use dd/MM/yyyy (ex: 08/04/2026) ou ISO.");
        return parsed.Day;
    }

    private static System.DateTime ParseFirstDate(string dateString, int dueDay)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            throw new DomainException("Data é obrigatória para despesa/receita fixa.");

        var formats = new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss" };
        if (!System.DateTime.TryParseExact(dateString.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
            throw new DomainException("Data inválida. Use dd/MM/yyyy (ex: 08/04/2026) ou ISO.");

        var day = Math.Min(dueDay, System.DateTime.DaysInMonth(parsed.Year, parsed.Month));
        return new System.DateTime(parsed.Year, parsed.Month, day);
    }

    public async Task<Transaction> GetByIdAsync(Guid transactionId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction is null)
            throw new DomainException("Transação não encontrada.");
        return transaction;
    }

    public async Task<PaginatedTransactionsResult> GetByUserAndMonthPagedAsync(GetAllTransactionByUserAndMonthQuery query)
    {
        if (query.Page < 1)
            throw new DomainException("Page must be at least 1.");

        var (items, totalCount) = await _transactionQueryRepository.GetByUserAndMonthPagedAsync(
            query.UserId,
            query.Month,
            query.Year,
            query.Page,
            TransactionsByMonthPageSize);

        return new PaginatedTransactionsResult(items, query.Page, TransactionsByMonthPageSize, totalCount);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query)
    {
        return await _transactionQueryRepository.GetByUserAndMonthAsync(query.UserId, query.Month, query.Year);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query)
    {
        return await _transactionQueryRepository.GetByUserAsync(query.UserId);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(GetTransactionsByAccountAndMonthYearQuery query)
    {
        return await _transactionQueryRepository.GetByAccountAndMonthAsync(query.UserId, query.AccountId, query.Month, query.Year);
    }

    public async Task<PaginatedTransactionsResult> GetByAccountAndMonthPagedAsync(GetTransactionsByAccountAndMonthYearQuery query, int page)
    {
        if (page < 1)
            throw new DomainException("Page must be at least 1.");

        var (items, totalCount) = await _transactionQueryRepository.GetByAccountAndMonthPagedAsync(
            query.UserId,
            query.AccountId,
            query.Month,
            query.Year,
            page,
            TransactionsByMonthPageSize);

        return new PaginatedTransactionsResult(items, page, TransactionsByMonthPageSize, totalCount);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetTransactionByCreditCardStatementAndMonthQuery(GetAllTransactionByCreditCardStatementAndMonthYearQuery query)
    {
        return await _transactionQueryRepository.GetAllTransactionByCreditCardStatementAndMonthYearQuery(query.userId, query.creditCardId, query.month, query.year);
    }

    public async Task UpdateStatusAsync(UpdateTransactionStatusCommand command)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId);

        if (transaction is null || transaction.UserId != command.UserId)
            throw new DomainException("Transação não encontrada.");

        var previousStatus = transaction.Status;

        if (command.Status == TransactionStatus.Pending && previousStatus == TransactionStatus.Completed)
        {
            var account = await _accountRepository.GetByIdAsync(transaction.AccountId);
            if (account is null)
                throw new DomainException("Conta não encontrada.");
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
                    throw new DomainException("Conta não encontrada.");
                TransactionApplier.Apply(account, transaction);
                await _accountRepository.UpdateAsync(account);
            }
        }

        await _transactionRepository.UpdateAsync(transaction);
    }

    public async Task<DeleteTransactionsResult> DeleteManyAsync(DeleteTransactionsCommand command)
    {
        if (command.TransactionIds.Count == 0)
            return new DeleteTransactionsResult(0, 0, Array.Empty<Guid>());

        var transactions = await _transactionRepository.GetByIdsAsync(command.TransactionIds);
        var toDelete = new List<Transaction>();
        var skippedIds = new List<Guid>();

        foreach (var transaction in transactions)
        {
            if (transaction.UserId != command.UserId)
            {
                skippedIds.Add(transaction.Id);
                continue;
            }

            if (transaction.Status == TransactionStatus.Completed)
            {
                skippedIds.Add(transaction.Id);
                continue;
            }

            toDelete.Add(transaction);
        }

        var requestedIds = command.TransactionIds.ToHashSet();
        foreach (var id in requestedIds)
        {
            if (!transactions.Any(t => t.Id == id))
                skippedIds.Add(id);
        }

        if (toDelete.Count > 0)
            await _transactionRepository.DeleteManyAsync(toDelete);

        return new DeleteTransactionsResult(
            toDelete.Count,
            skippedIds.Count,
            skippedIds);
    }

}