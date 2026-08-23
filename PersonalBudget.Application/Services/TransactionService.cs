using System.Globalization;
using System.Text.RegularExpressions;
using PersonalBudget.Application.DTOs.Transaction;
using PersonalBudget.Application.Interfaces;

public class TransactionService : ITransactionService
{
    public const int TransactionsByMonthPageSize = 15;

    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionQueryRepository _transactionQueryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICreditCardStatementRepository _creditCardStatementRepository;
    private readonly IHouseholdMemberProfileRepository _profileRepository;
    private readonly IReadOnlyDictionary<PaymentMethod, ITransactionCreationStrategy> _creationStrategies;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ITransactionQueryRepository transactionQueryRepository,
        IAccountRepository accountRepository,
        ICreditCardRepository creditCardRepository,
        ICreditCardStatementRepository creditCardStatementRepository,
        IHouseholdMemberProfileRepository profileRepository,
        IEnumerable<ITransactionCreationStrategy> creationStrategies)
    {
        _transactionRepository = transactionRepository;
        _transactionQueryRepository = transactionQueryRepository;
        _accountRepository = accountRepository;
        _creditCardRepository = creditCardRepository;
        _creditCardStatementRepository = creditCardStatementRepository;
        _profileRepository = profileRepository;
        _creationStrategies = creationStrategies.ToDictionary(s => s.PaymentMethod);
    }

    public async Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
        var resolvedProfile = await ResolveAttributionProfileIdAsync(command);
        var withProfile = command with { AttributionProfileId = resolvedProfile };

        ValidateTransactionFrequency(withProfile);
        ValidateCategoryId(withProfile);

        if (!_creationStrategies.TryGetValue(withProfile.PaymentMethod, out var strategy))
            throw new DomainException($"Método de pagamento não suportado: {withProfile.PaymentMethod}");

        var repeatCount = withProfile.RepeatCount ?? 1;
        if (repeatCount > 1)
        {
            if (withProfile.PaymentMethod != PaymentMethod.Account)
                throw new DomainException(String.Format("{0} recorrente só é permitida com método de pagamento Conta. PaymentMethod: {1}", withProfile.Type, withProfile.PaymentMethod));
            var dueDay = withProfile.DueDay ?? ParseDayFromDate(withProfile.Date);
            if (dueDay is < 1 or > 31)
                throw new DomainException("Para recorrência, Data de vencimento deve ser entre 1 e 31.");
            return await CreateRecurringAsync(withProfile with { DueDay = dueDay }, strategy, repeatCount);
        }

        return await strategy.CreateAsync(withProfile);
    }

    private async Task<Guid> ResolveAttributionProfileIdAsync(CreateTransactionCommand command)
    {
        if (command.AttributionProfileId is { } pid && pid != Guid.Empty)
        {
            var p = await _profileRepository.GetByIdAsync(pid);
            if (p is null || p.HouseholdId != command.HouseholdId)
                throw new DomainException("Correspondente inválido para este lar.");
            return pid;
        }

        var linked = await _profileRepository.GetLinkedProfileForUserAsync(command.HouseholdId, command.UserId);
        if (linked is null)
            throw new DomainException("Não há perfil de correspondente vinculado ao seu usuário neste lar.");

        return linked.Id;
    }

    private static void ValidateCategoryId(CreateTransactionCommand command)
    {
        if (command.PaymentMethod != PaymentMethod.Transfer && command.CategoryId is null)
            throw new DomainException("Categoria é obrigatória.");
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
        var createdIds = new List<Guid>();

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
                DueDay = null,
                DueDate = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            };

            var id = await strategy.CreateAsync(singleCommand);
            createdIds.Add(id);
        }

        var recurrenceId = Guid.NewGuid();
        var transactions = (await _transactionRepository.GetByIdsAsync(createdIds)).ToList();
        foreach (var t in transactions)
            t.AssignRecurrenceId(recurrenceId);
        await _transactionRepository.BulkUpdateAsync(transactions);

        return createdIds[0];
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

    public async Task<Transaction> GetByIdAsync(Guid transactionId, Guid householdId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction is null || transaction.HouseholdId != householdId)
            throw new DomainException("Transação não encontrada.");
        return transaction;
    }

    public async Task<PaginatedTransactionsResult> GetByHouseholdAndMonthPagedAsync(GetAllTransactionByHouseholdAndMonthQuery query)
    {
        if (query.Page < 1)
            throw new DomainException("Page must be at least 1.");

        var (items, totalCount, periodIncome, periodExpense) = await _transactionQueryRepository.GetByHouseholdAndMonthPagedAsync(
            query.HouseholdId,
            query.Month,
            query.Year,
            query.Page,
            TransactionsByMonthPageSize,
            query.Frequency);

        return new PaginatedTransactionsResult(items, query.Page, TransactionsByMonthPageSize, totalCount, periodIncome, periodExpense);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByHouseholdAndMonthAsync(GetAllTransactionByHouseholdAndMonthQuery query)
    {
        return await _transactionQueryRepository.GetByHouseholdAndMonthAsync(query.HouseholdId, query.Month, query.Year, query.Frequency);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByHouseholdAsync(GetAllTransactionByHouseholdQuery query)
    {
        return await _transactionQueryRepository.GetByHouseholdAsync(query.HouseholdId);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(GetTransactionsByAccountAndMonthYearQuery query)
    {
        return await _transactionQueryRepository.GetByAccountAndMonthAsync(query.HouseholdId, query.AccountId, query.Month, query.Year, query.Frequency);
    }

    public async Task<PaginatedTransactionsResult> GetByAccountAndMonthPagedAsync(GetTransactionsByAccountAndMonthYearQuery query, int page)
    {
        if (page < 1)
            throw new DomainException("Page must be at least 1.");

        var (items, totalCount) = await _transactionQueryRepository.GetByAccountAndMonthPagedAsync(
            query.HouseholdId,
            query.AccountId,
            query.Month,
            query.Year,
            page,
            TransactionsByMonthPageSize,
            query.Frequency);

        return new PaginatedTransactionsResult(items, page, TransactionsByMonthPageSize, totalCount, 0m, 0m);
    }

    public Task<TransactionsCategoryGroupResponse> GetGroupedByCategoryForMonthAsync(Guid householdId, int month, int year)
        => _transactionQueryRepository.GetGroupedByCategoryForMonthAsync(householdId, month, year);

    private static readonly Regex InstallmentPattern = new(@"^(.+)\s+\((\d+)/(\d+)\)$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<ActiveInstallmentGroupDto>> GetActiveInstallmentsAsync(Guid householdId, DateTime upTo)
    {
        var all = await _transactionQueryRepository.GetInstallmentTransactionsAsync(householdId);

        // Group by (creditCardId, base description, total count extracted from suffix)
        var groups = new Dictionary<string, List<GetAllTransactionByUserResponse>>();
        foreach (var t in all)
        {
            var key = BuildInstallmentGroupKey(t);
            if (!groups.TryGetValue(key, out var list))
                groups[key] = list = [];
            list.Add(t);
        }

        var result = new List<ActiveInstallmentGroupDto>();
        foreach (var (groupKey, items) in groups)
        {
            // Only include groups that have at least one pending installment due <= upTo
            var pendingUpTo = items
                .Where(t => t.Status != TransactionStatus.Completed.ToString()
                         && t.Status != TransactionStatus.Cancelled.ToString()
                         && t.Date <= upTo)
                .ToList();

            if (pendingUpTo.Count == 0)
                continue;

            var first = items[0];
            var (baseDesc, _, total) = ParseInstallmentDescription(first.Description);
            var installmentsTotal = total > 0 ? total : items.Count;
            var installmentsPaid = items.Count(t => t.Status == TransactionStatus.Completed.ToString());
            var pendingItems = items.Where(t => t.Status != TransactionStatus.Completed.ToString()
                                             && t.Status != TransactionStatus.Cancelled.ToString()).ToList();
            var remainingAmount = pendingItems.Sum(t => t.Amount);
            var nextDue = pendingItems.OrderBy(t => t.Date).FirstOrDefault();
            var installmentAmount = items.OrderBy(t => t.Date).First().Amount;
            var totalAmount = items.Sum(t => t.Amount);

            result.Add(new ActiveInstallmentGroupDto(
                GroupKey: groupKey,
                Description: baseDesc,
                CreditCardId: first.CreditCardId,
                CreditCardName: first.CreditCardName,
                CategoryId: first.CategoryId,
                CategoryName: first.CategoryName,
                InstallmentAmount: installmentAmount,
                TotalAmount: totalAmount,
                InstallmentsPaid: installmentsPaid,
                InstallmentsTotal: installmentsTotal,
                RemainingAmount: remainingAmount,
                NextDueDate: nextDue?.Date.ToString("yyyy-MM-dd"),
                Items: items));
        }

        return result.OrderBy(g => g.NextDueDate).ToList();
    }

    private static string BuildInstallmentGroupKey(GetAllTransactionByUserResponse t)
    {
        var (baseDesc, _, total) = ParseInstallmentDescription(t.Description);
        return $"{t.CreditCardId}|{baseDesc}|{total}";
    }

    private static (string BaseDescription, int Current, int Total) ParseInstallmentDescription(string description)
    {
        var match = InstallmentPattern.Match(description);
        if (match.Success
            && int.TryParse(match.Groups[2].Value, out var current)
            && int.TryParse(match.Groups[3].Value, out var total))
        {
            return (match.Groups[1].Value, current, total);
        }
        return (description, 0, 0);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetTransactionByCreditCardStatementAndMonthQuery(GetAllTransactionByCreditCardStatementAndMonthYearQuery query)
    {
        return await _transactionQueryRepository.GetAllTransactionByCreditCardStatementAndMonthYearQuery(
            query.HouseholdId, query.CreditCardId, query.Month, query.Year);
    }

    public async Task UpdateAsync(UpdateTransactionCommand command)
    {
        var hasAny = command.Amount.HasValue
            || command.Date is not null
            || command.Description is not null
            || command.CategoryId is not null
            || command.DueDate is not null
            || command.ExpirationDate is not null
            || command.AttributionProfileId is not null
            || command.StatementMonth is not null
            || command.StatementYear is not null
            || command.Observations is not null;

        if (!hasAny)
            throw new DomainException("Informe ao menos um campo para atualizar.");

        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId);
        if (transaction is null || transaction.HouseholdId != command.HouseholdId)
            throw new DomainException("Transação não encontrada.");

        EnsureEditableForDetails(transaction);

        var isCreditCard = transaction.PaymentMethod == PaymentMethod.CreditCard || transaction.CreditCardId is not null;
        if (isCreditCard)
            await EnsureCreditCardStatementIsOpenForMutationAsync(transaction, "editar");

        if (command.AttributionProfileId is { } pid && pid != Guid.Empty)
        {
            var p = await _profileRepository.GetByIdAsync(pid);
            if (p is null || p.HouseholdId != command.HouseholdId)
                throw new DomainException("Correspondente inválido para este lar.");
        }

        if (command.StatementMonth.HasValue && (command.StatementMonth < 1 || command.StatementMonth > 12))
            throw new DomainException("StatementMonth deve estar entre 1 e 12.");

        // StatementMonth e StatementYear devem ser informados juntos
        if (command.StatementMonth.HasValue != command.StatementYear.HasValue)
            throw new DomainException("StatementMonth e StatementYear devem ser informados juntos.");

        var newAmount = command.Amount ?? transaction.Amount.Amount;
        var newDateVo = command.Date is null
            ? transaction.Date
            : new TransactionDate(ParseDateTimeForUpdate(command.Date));
        var newDescription = command.Description is null
            ? transaction.Description
            : new TransactionDescription(command.Description);
        var newCategoryId = command.CategoryId ?? transaction.CategoryId;

        var newDue = ResolveOptionalDateField(command.DueDate, transaction.DueDate);
        var newExpiration = ResolveOptionalDateField(command.ExpirationDate, transaction.ExpirationDate);

        if (newExpiration is not null && transaction.Frequency != TransactionFrequency.Fixed)
            throw new DomainException("ExpirationDate só é permitido com TransactionFrequency.Fixed.");

        if (isCreditCard)
            await ApplyCreditCardStatementAdjustmentsAsync(transaction, command.HouseholdId, newAmount, command.StatementMonth, command.StatementYear);

        transaction.UpdateDetails(
            new Money(newAmount),
            newDateVo,
            newDescription,
            newCategoryId,
            newExpiration,
            newDue,
            observations: command.Observations,
            updateObservations: command.Observations is not null);

        if (command.AttributionProfileId is { } apid && apid != Guid.Empty && apid != transaction.AttributionProfileId)
            transaction.UpdateAttributionProfileId(apid);

        await _transactionRepository.UpdateAsync(transaction);
    }

    public async Task UpdateRecurringAsync(UpdateRecurringTransactionCommand command)
    {
        var hasAny = command.Amount.HasValue
            || command.Date is not null
            || command.Description is not null
            || command.CategoryId is not null
            || command.DueDate is not null
            || command.ExpirationDate is not null
            || command.AttributionProfileId is not null;

        if (!hasAny)
            throw new DomainException("Informe ao menos um campo para atualizar.");

        var pivot = await _transactionRepository.GetByIdAsync(command.TransactionId);
        if (pivot is null || pivot.HouseholdId != command.HouseholdId)
            throw new DomainException("Transação não encontrada.");

        if (command.RecurrenceEditMode == RecurrenceEditMode.OnlyThis || pivot.RecurrenceId is null)
        {
            var singleCommand = new UpdateTransactionCommand(
                command.HouseholdId,
                command.TransactionId,
                command.Amount,
                command.Date,
                command.Description,
                command.CategoryId,
                command.DueDate,
                command.ExpirationDate,
                command.AttributionProfileId);
            await UpdateAsync(singleCommand);
            return;
        }

        var allInSeries = await _transactionRepository.GetByRecurrenceIdAsync(
            pivot.RecurrenceId.Value, command.HouseholdId);

        var targets = command.RecurrenceEditMode switch
        {
            RecurrenceEditMode.ThisAndFuture =>
                allInSeries.Where(t => t.Date.Value >= pivot.Date.Value).ToList(),
            RecurrenceEditMode.All =>
                allInSeries.ToList(),
            _ => throw new DomainException("RecurrenceEditMode inválido.")
        };

        foreach (var t in targets)
            EnsureEditableForDetails(t);

        var newExpiration = ResolveOptionalDateField(command.ExpirationDate, targets[0].ExpirationDate);
        if (newExpiration is not null)
        {
            foreach (var t in targets)
            {
                if (t.Frequency != TransactionFrequency.Fixed)
                    throw new DomainException("ExpirationDate só é permitido com TransactionFrequency.Fixed.");
            }
        }

        if (command.AttributionProfileId is { } pid && pid != Guid.Empty)
        {
            var p = await _profileRepository.GetByIdAsync(pid);
            if (p is null || p.HouseholdId != command.HouseholdId)
                throw new DomainException("Correspondente inválido para este lar.");
        }

        int? newDay = command.Date is null ? null : ParseDateTimeForUpdate(command.Date).Day;

        foreach (var t in targets)
        {
            var newAmount = command.Amount ?? t.Amount.Amount;
            var newDateVo = newDay is null
                ? t.Date
                : new TransactionDate(ApplyDayToExistingMonth(newDay.Value, t.Date.Value));
            var newDescription = command.Description is null
                ? t.Description
                : new TransactionDescription(BuildDescriptionPreservingInstallmentSuffix(command.Description, t.Description.Value));
            var newCategoryId = command.CategoryId ?? t.CategoryId;
            var newDue = ResolveOptionalDateField(command.DueDate, t.DueDate);
            var newExp = ResolveOptionalDateField(command.ExpirationDate, t.ExpirationDate);

            t.UpdateDetails(new Money(newAmount), newDateVo, newDescription, newCategoryId, newExp, newDue);

            if (command.AttributionProfileId is { } apid && apid != Guid.Empty && apid != t.AttributionProfileId)
                t.UpdateAttributionProfileId(apid);
        }

        await _transactionRepository.BulkUpdateAsync(targets);
    }

    public Task<DeleteTransactionsResult> DeleteAsync(DeleteTransactionCommand command)
        => DeleteManyAsync(new DeleteTransactionsCommand(command.HouseholdId, new[] { command.TransactionId }));

    public async Task<DeleteTransactionsResult> DeleteRecurringAsync(DeleteRecurringTransactionCommand command)
    {
        var pivot = await _transactionRepository.GetByIdAsync(command.TransactionId);
        if (pivot is null || pivot.HouseholdId != command.HouseholdId)
            throw new DomainException("Transação não encontrada.");

        if (pivot.RecurrenceId is null || command.RecurrenceDeleteMode == RecurrenceDeleteMode.OnlyThis)
            return await DeleteAsync(new DeleteTransactionCommand(command.HouseholdId, command.TransactionId));

        var allInSeries = await _transactionRepository.GetByRecurrenceIdAsync(
            pivot.RecurrenceId.Value, command.HouseholdId);

        var targets = command.RecurrenceDeleteMode switch
        {
            RecurrenceDeleteMode.ThisAndFuture =>
                allInSeries.Where(t => t.Date.Value >= pivot.Date.Value).ToList(),
            RecurrenceDeleteMode.All =>
                allInSeries.ToList(),
            _ => throw new DomainException("RecurrenceDeleteMode inválido.")
        };

        var ids = targets.Select(t => t.Id).ToList();
        return await DeleteManyAsync(new DeleteTransactionsCommand(command.HouseholdId, ids));
    }

    private static void EnsureEditableForDetails(Transaction transaction)
    {
        if (transaction.Status == TransactionStatus.Completed)
            throw new DomainException("Transações concluídas não podem ser editadas.");

        if (transaction.TransferId is not null || transaction.PaymentMethod == PaymentMethod.Transfer)
            throw new DomainException("Transferências não podem ser editadas por esta operação.");
    }

    /// <param name="deniedActionVerb">Frase no infinitivo, ex.: "editar", "excluir", "alterar o status de".</param>
    private async Task EnsureCreditCardStatementIsOpenForMutationAsync(Transaction transaction, string deniedActionVerb)
    {
        var isCreditCard = transaction.PaymentMethod == PaymentMethod.CreditCard || transaction.CreditCardId is not null;
        if (!isCreditCard)
            return;

        if (transaction.StatementId is null)
            throw new DomainException("Transação de cartão sem fatura associada.");

        var statement = await _creditCardStatementRepository.GetByIdAsync(transaction.StatementId.Value);
        if (statement is null)
            throw new DomainException("Fatura não encontrada.");

        if (statement.Status != BillStatus.Open)
            throw new DomainException($"Não é possível {deniedActionVerb} transações de cartão em fatura fechada ou paga.");
    }

    private async Task ApplyCreditCardStatementAdjustmentsAsync(
        Transaction transaction,
        Guid householdId,
        decimal newAmount,
        int? statementMonth,
        int? statementYear)
    {
        var creditCard = await _creditCardRepository.GetByIdAsync(transaction.CreditCardId!.Value);
        if (creditCard is null || creditCard.HouseholdId != householdId)
            throw new DomainException("Cartão de crédito não encontrado.");

        var oldStatement = await _creditCardStatementRepository.GetByIdAsync(transaction.StatementId!.Value);
        if (oldStatement is null)
            throw new DomainException("Fatura não encontrada.");

        if (oldStatement.Status != BillStatus.Open)
            throw new DomainException("Não é possível editar transações de cartão em fatura fechada ou paga.");

        var newMoney = new Money(newAmount);

        // Se mês/ano de fatura não foram informados, mantém na mesma fatura
        if (statementMonth is null || statementYear is null)
        {
            if (transaction.Amount.Amount != newAmount)
            {
                oldStatement.ReplaceTransactionContribution(transaction.Amount, newMoney, transaction.Type);
                await _creditCardStatementRepository.UpdateAsync(oldStatement);
            }
            return;
        }

        CreditCardStatement targetStatement;
        var createdNewStatement = false;

        var existing = await _creditCardStatementRepository.GetByCreditCardAndClosingMonthYearAsync(
            creditCard.Id, statementMonth.Value, statementYear.Value);

        if (existing is not null)
        {
            targetStatement = existing;
        }
        else
        {
            targetStatement = CreditCardStatement.CreateForMonth(
                creditCard.Id, statementMonth.Value, statementYear.Value,
                creditCard.ClosingDay, creditCard.DueDay);
            targetStatement.AddTransaction(newMoney, transaction.Type);
            await _creditCardStatementRepository.AddAsync(targetStatement);
            createdNewStatement = true;
        }

        var movingToDifferentStatement = targetStatement.Id != oldStatement.Id;

        if (movingToDifferentStatement)
        {
            oldStatement.RemoveTransactionContribution(transaction.Amount, transaction.Type);

            if (!createdNewStatement)
                targetStatement.AddTransaction(newMoney, transaction.Type);

            await _creditCardStatementRepository.UpdateAsync(oldStatement);
            await _creditCardStatementRepository.UpdateAsync(targetStatement);

            transaction.ChangeCreditCardStatement(targetStatement.Id);
        }
        else if (transaction.Amount.Amount != newAmount)
        {
            oldStatement.ReplaceTransactionContribution(transaction.Amount, newMoney, transaction.Type);
            await _creditCardStatementRepository.UpdateAsync(oldStatement);
        }
    }

    private static DateTime? ResolveOptionalDateField(string? commandValue, DateTime? current)
    {
        if (commandValue is null)
            return current;

        if (string.IsNullOrWhiteSpace(commandValue))
            return null;

        return DateTime.SpecifyKind(ParseDateTimeForUpdate(commandValue).Date, DateTimeKind.Utc);
    }

    private static string BuildDescriptionPreservingInstallmentSuffix(string newBase, string existing)
    {
        var (strippedBase, _, _) = ParseInstallmentDescription(newBase);
        var (_, current, total) = ParseInstallmentDescription(existing);
        return current > 0 && total > 0
            ? $"{strippedBase} ({current}/{total})"
            : strippedBase;
    }

    private static DateTime ApplyDayToExistingMonth(int day, DateTime existing)
    {
        var safeDay = Math.Min(day, DateTime.DaysInMonth(existing.Year, existing.Month));
        return DateTime.SpecifyKind(new DateTime(existing.Year, existing.Month, safeDay), DateTimeKind.Utc);
    }

    private static DateTime ParseDateTimeForUpdate(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            throw new DomainException("Data inválida.");

        var trimmed = dateString.Trim();
        var formats = new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss" };
        if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        if (DateTime.TryParseExact(trimmed, new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm" }, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var parsedBr))
            return DateTime.SpecifyKind(parsedBr, DateTimeKind.Utc);

        throw new DomainException("Data inválida. Use dd/MM/yyyy (ex: 08/04/2026) ou ISO.");
    }

    public async Task UpdateInstallmentStatementAsync(UpdateInstallmentStatementCommand command)
    {
        if (command.StatementMonth < 1 || command.StatementMonth > 12)
            throw new DomainException("StatementMonth deve estar entre 1 e 12.");

        var pivot = await _transactionRepository.GetByIdAsync(command.TransactionId);
        if (pivot is null || pivot.HouseholdId != command.HouseholdId)
            throw new DomainException("Transação não encontrada.");

        if (pivot.Frequency != TransactionFrequency.Installments)
            throw new DomainException("Esta operação só é permitida para transações parceladas.");

        if (pivot.RecurrenceId is null)
            throw new DomainException("Transação parcelada sem grupo de parcelas associado.");

        if (pivot.CreditCardId is null || pivot.StatementId is null)
            throw new DomainException("Transação sem dados de cartão/fatura associados.");

        var creditCard = await _creditCardRepository.GetByIdAsync(pivot.CreditCardId.Value);
        if (creditCard is null || creditCard.HouseholdId != command.HouseholdId)
            throw new DomainException("Cartão de crédito não encontrado.");

        var pivotStatement = await _creditCardStatementRepository.GetByIdAsync(pivot.StatementId.Value);
        if (pivotStatement is null)
            throw new DomainException("Fatura atual da parcela não encontrada.");

        // Calcula o delta em meses entre a fatura atual do pivô e a nova fatura desejada
        var pivotMonths = pivotStatement.ClosingYear * 12 + (pivotStatement.ClosingMonth - 1);
        var targetMonths = command.StatementYear * 12 + (command.StatementMonth - 1);
        var monthDelta = targetMonths - pivotMonths;

        if (monthDelta == 0)
            return;

        var allInSeries = await _transactionRepository.GetByRecurrenceIdAsync(pivot.RecurrenceId.Value, command.HouseholdId);
        var orderedSeries = allInSeries.OrderBy(t => t.Date.Value).ToList();

        var targets = command.EditMode switch
        {
            InstallmentEditMode.All => orderedSeries,
            InstallmentEditMode.ThisAndFuture => orderedSeries.Where(t => t.Date.Value >= pivot.Date.Value).ToList(),
            _ => throw new DomainException("InstallmentEditMode inválido.")
        };

        foreach (var t in targets)
        {
            if (t.Status == TransactionStatus.Completed)
                throw new DomainException("Não é possível mover parcelas já concluídas.");
            if (t.StatementId is null)
                throw new DomainException("Parcela sem fatura associada.");
        }

        // Carrega todas as faturas atuais e valida que estão abertas
        // O cache unificado por (mês, ano) evita dupla contagem quando uma fatura é
        // ao mesmo tempo origem de uma parcela e destino de outra (e.g. delta negativo)
        var statementByMonthYear = new Dictionary<(int Month, int Year), CreditCardStatement>();
        var statementsById = new Dictionary<Guid, CreditCardStatement>();

        foreach (var id in targets.Select(t => t.StatementId!.Value).Distinct())
        {
            var s = await _creditCardStatementRepository.GetByIdAsync(id);
            if (s is null)
                throw new DomainException("Fatura não encontrada.");
            if (s.Status != BillStatus.Open)
                throw new DomainException("Não é possível mover parcelas em fatura fechada ou paga.");
            statementsById[id] = s;
            statementByMonthYear[(s.ClosingMonth, s.ClosingYear)] = s;
        }

        var statementsToUpdate = new Dictionary<Guid, CreditCardStatement>();
        var modifiedTransactions = new List<Transaction>();

        foreach (var t in targets)
        {
            var oldStatement = statementsById[t.StatementId!.Value];

            var oldMonths = oldStatement.ClosingYear * 12 + (oldStatement.ClosingMonth - 1);
            var newTotalMonths = oldMonths + monthDelta;

            // Aritmética de meses segura para deltas negativos
            var newYear = newTotalMonths / 12;
            var newMonthRemainder = newTotalMonths % 12;
            if (newMonthRemainder < 0)
            {
                newMonthRemainder += 12;
                newYear--;
            }
            var newMonth = newMonthRemainder + 1; // volta para base 1

            var cacheKey = (newMonth, newYear);
            if (!statementByMonthYear.TryGetValue(cacheKey, out var targetStatement))
            {
                var existing = await _creditCardStatementRepository.GetByCreditCardAndClosingMonthYearAsync(
                    creditCard.Id, newMonth, newYear);

                if (existing is not null)
                {
                    if (existing.Status != BillStatus.Open)
                        throw new DomainException($"A fatura de destino {newMonth}/{newYear} está fechada ou paga.");
                    targetStatement = existing;
                }
                else
                {
                    targetStatement = CreditCardStatement.CreateForMonth(
                        creditCard.Id, newMonth, newYear,
                        creditCard.ClosingDay, creditCard.DueDay);
                    await _creditCardStatementRepository.AddAsync(targetStatement);
                }

                statementByMonthYear[cacheKey] = targetStatement;
                statementsById[targetStatement.Id] = targetStatement;
            }

            if (targetStatement.Id == oldStatement.Id)
                continue;

            oldStatement.RemoveTransactionContribution(t.Amount, t.Type);
            targetStatement.AddTransaction(t.Amount, t.Type);
            t.ChangeCreditCardStatement(targetStatement.Id);

            // Desloca a data da transação pelo mesmo delta para manter a coerência visual
            var newDate = DateTime.SpecifyKind(t.Date.Value.AddMonths(monthDelta), DateTimeKind.Utc);
            t.UpdateDetails(t.Amount, new TransactionDate(newDate), t.Description, t.CategoryId, t.ExpirationDate, t.DueDate);

            statementsToUpdate[oldStatement.Id] = oldStatement;
            statementsToUpdate[targetStatement.Id] = targetStatement;
            modifiedTransactions.Add(t);
        }

        foreach (var s in statementsToUpdate.Values)
            await _creditCardStatementRepository.UpdateAsync(s);

        if (modifiedTransactions.Count > 0)
            await _transactionRepository.BulkUpdateAsync(modifiedTransactions);
    }

    public async Task UpdateStatusAsync(UpdateTransactionStatusCommand command)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId);

        if (transaction is null || transaction.HouseholdId != command.HouseholdId)
            throw new DomainException("Transação não encontrada.");

        await EnsureCreditCardStatementIsOpenForMutationAsync(transaction, "alterar o status de");

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
            if (transaction.HouseholdId != command.HouseholdId)
            {
                skippedIds.Add(transaction.Id);
                continue;
            }

            if (transaction.Status == TransactionStatus.Completed)
            {
                skippedIds.Add(transaction.Id);
                continue;
            }

            await EnsureCreditCardStatementIsOpenForMutationAsync(transaction, "excluir");

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
