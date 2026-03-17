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

    public async Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
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

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query)
    {
        return await _transactionQueryRepository.GetByUserAndMonthAsync(query.UserId, query.Month, query.Year);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query)
    {
        return await _transactionQueryRepository.GetByUserAsync(query.UserId);
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

}