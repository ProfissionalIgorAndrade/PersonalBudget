using PersonalBudget.Application.DTOs.Account;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;
    private readonly IHouseholdMemberProfileRepository _profileRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISavingsBoxEventRepository _eventRepository;

    public AccountService(
        IAccountRepository repository,
        IHouseholdMemberProfileRepository profileRepository,
        ITransactionRepository transactionRepository,
        ISavingsBoxEventRepository eventRepository)
    {
        _repository = repository;
        _profileRepository = profileRepository;
        _transactionRepository = transactionRepository;
        _eventRepository = eventRepository;
    }

    /// <summary>
    /// Grava o movimento da caixinha como lançamento para manter extrato completo.
    /// PaymentMethod.Savings mantém o lançamento fora dos totais de receita e despesa.
    /// </summary>
    private async Task RecordSavingsMovementAsync(
        Account box, decimal amount, bool isDeposit, string? reason, string? description = null)
    {
        var profile = box.MemberProfileId;
        if (profile is null) return;

        var tx = Transaction.Create(
            userId: box.UserId,
            householdId: box.HouseholdId,
            attributionProfileId: profile.Value,
            accountId: box.Id,
            amount: new Money(amount),
            type: isDeposit ? TransactionType.Income : TransactionType.Expense,
            paymentMethod: PaymentMethod.Savings,
            date: DateTime.UtcNow.Date,
            description: description ?? (isDeposit ? $"Depósito em {box.Name}" : $"Resgate de {box.Name}"),
            observations: string.IsNullOrWhiteSpace(reason) ? null : reason.Trim());

        await _transactionRepository.AddAsync(tx);
    }

    /// <summary>
    /// Carrega a caixinha garantindo que existe, pertence ao lar e é de fato uma caixinha.
    /// </summary>
    private async Task<Account> GetSavingsBoxOrThrowAsync(Guid householdId, Guid accountId)
    {
        var box = await _repository.GetByIdAsync(accountId)
            ?? throw new DomainException("Caixinha não encontrada.");

        if (box.HouseholdId != householdId)
            throw new DomainException("Caixinha não pertence a este lar.");

        if (box.Kind != AccountKind.Savings)
            throw new DomainException("Esta operação é exclusiva para caixinhas.");

        return box;
    }

    public async Task<Guid> CreateAsync(CreateAccountCommand command)
    {
        var account = Account.Create(
            command.UserId,
            command.HouseholdId,
            command.Bank,
            command.Name,
            command.MemberId
        );

        await _repository.AddAsync(account);
        return account.Id;
    }

    public async Task<IEnumerable<AccountResponse>> GetByHouseholdAsync(Guid householdId)
    {
        var accounts = await _repository.GetByHouseholdIdAsync(householdId);
        var profiles = await _profileRepository.GetByHouseholdAsync(householdId);
        var profileMap = profiles.ToDictionary(p => p.Id, p => p.DisplayName);

        var accountIds = accounts.Select(a => a.Id);
        var balances = await _transactionRepository.GetBalancesByAccountIdsAsync(accountIds);

        return accounts.Select(a =>
        {
            string? memberName = a.MemberProfileId.HasValue && profileMap.TryGetValue(a.MemberProfileId.Value, out var n) ? n : null;
            var displayName = AccountDisplayName.Build(a, memberName);
            var balance = balances.GetValueOrDefault(a.Id, 0m);
            return new AccountResponse(
                a.Id,
                a.Bank.ToString(),
                balance,
                a.MemberProfileId,
                memberName,
                displayName,
                a.IsActive,
                a.CreatedAt,
                a.Kind.ToString(),
                a.ParentAccountId,
                a.Name,
                a.SavingsGoal
            );
        });
    }

    public async Task<AccountsSummaryResponse> GetSummaryAsync(Guid householdId)
    {
        var accounts = await _repository.GetByHouseholdIdAsync(householdId);
        var profiles = await _profileRepository.GetByHouseholdAsync(householdId);
        var profileMap = profiles.ToDictionary(p => p.Id, p => p.DisplayName);

        var active = accounts.Where(a => a.IsActive).ToList();
        var accountIds = active.Select(a => a.Id);
        var balances = await _transactionRepository.GetBalancesByAccountIdsAsync(accountIds);

        var totalBalance = active.Sum(a => balances.GetValueOrDefault(a.Id, 0m));
        var items = active
            .Select(a =>
            {
                string? memberName = a.MemberProfileId.HasValue && profileMap.TryGetValue(a.MemberProfileId.Value, out var n) ? n : null;
                return new AccountSummaryItem(a.Id, AccountDisplayName.Build(a, memberName), a.Bank.ToString(), balances.GetValueOrDefault(a.Id, 0m));
            })
            .ToList();
        return new AccountsSummaryResponse(totalBalance, items);
    }

    public async Task<Guid> CreateSavingsBoxAsync(CreateSavingsBoxCommand command)
    {
        var parent = await _repository.GetByIdAsync(command.ParentAccountId)
            ?? throw new DomainException("Conta de origem não encontrada.");

        if (parent.HouseholdId != command.HouseholdId)
            throw new DomainException("Conta de origem não pertence a este lar.");

        var box = Account.CreateSavingsBox(parent, command.Name);

        await _repository.AddAsync(box);
        await _eventRepository.AddAsync(
            SavingsBoxEvent.Created(box.HouseholdId, command.UserId, box.Id, box.Name!));
        return box.Id;
    }

    public async Task RenameSavingsBoxAsync(RenameSavingsBoxCommand command)
    {
        var box = await GetSavingsBoxOrThrowAsync(command.HouseholdId, command.AccountId);

        box.RenameSavingsBox(command.Name);
        await _repository.UpdateAsync(box);
    }

    public async Task SetSavingsGoalAsync(SetSavingsGoalCommand command)
    {
        var box = await GetSavingsBoxOrThrowAsync(command.HouseholdId, command.AccountId);

        box.SetSavingsGoal(command.Goal);
        await _repository.UpdateAsync(box);
    }

    public async Task DepositToSavingsBoxAsync(DepositToSavingsBoxCommand command)
    {
        var box = await GetSavingsBoxOrThrowAsync(command.HouseholdId, command.AccountId);

        await RecordSavingsMovementAsync(box, command.Amount, isDeposit: true, command.Reason);
    }

    public async Task WithdrawFromSavingsBoxAsync(WithdrawFromSavingsBoxCommand command)
    {
        var box = await GetSavingsBoxOrThrowAsync(command.HouseholdId, command.AccountId);

        await RecordSavingsMovementAsync(box, command.Amount, isDeposit: false, command.Reason);
    }

    /// <summary>
    /// Exclui a caixinha: resgata o saldo, deposita em outra caixinha, registra o evento e desativa.
    ///
    /// Limitação: cada repositório grava sozinho (SaveChanges por chamada) e não há unit of work,
    /// então a sequência não é uma única transação. Por isso tudo é validado, e o evento é
    /// construído (validando motivo e nomes), antes da primeira gravação. A ordem é resgate,
    /// depósito, evento e desativação: uma falha de banco no meio pode deixar o saldo já movido
    /// com a caixinha ainda ativa (saldo zerado, nunca duplicado).
    /// </summary>
    public async Task DeleteSavingsBoxAsync(DeleteSavingsBoxCommand command)
    {
        var box = await GetSavingsBoxOrThrowAsync(command.HouseholdId, command.AccountId);

        if (!box.IsActive)
            throw new DomainException("A caixinha já foi excluída.");

        var reason = command.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            throw new DomainException("Informe o motivo da exclusão da caixinha.");
        if (reason.Length > SavingsBoxEvent.ReasonMaxLength)
            throw new DomainException($"O motivo deve ter no máximo {SavingsBoxEvent.ReasonMaxLength} caracteres.");

        var balances = await _transactionRepository.GetBalancesByAccountIdsAsync(new[] { box.Id });
        var balance = balances.GetValueOrDefault(box.Id, 0m);
        var amountToMove = balance > 0 ? balance : 0m;

        Account? destination = null;
        if (amountToMove > 0)
        {
            if (command.DestinationAccountId is null)
                throw new DomainException("Informe a caixinha de destino para mover o saldo.");
            if (command.DestinationAccountId == box.Id)
                throw new DomainException("A caixinha de destino deve ser diferente da caixinha excluída.");

            destination = await _repository.GetByIdAsync(command.DestinationAccountId.Value);
            if (destination is null
                || destination.HouseholdId != command.HouseholdId
                || destination.Kind != AccountKind.Savings
                || !destination.IsActive)
                throw new DomainException("A caixinha de destino não foi encontrada.");

            if (box.MemberProfileId is null || destination.MemberProfileId is null)
                throw new DomainException("As caixinhas precisam estar vinculadas a um membro da família.");
        }

        var savingsBoxEvent = SavingsBoxEvent.Deleted(
            box.HouseholdId,
            command.UserId,
            box.Id,
            box.Name!,
            reason,
            amountToMove,
            destination?.Id,
            destination?.Name);

        if (destination is not null)
        {
            await RecordSavingsMovementAsync(box, amountToMove, isDeposit: false, reason,
                description: $"Resgate por exclusão da caixinha {box.Name}");
            await RecordSavingsMovementAsync(destination, amountToMove, isDeposit: true, reason,
                description: $"Recebido da caixinha excluída {box.Name}");
        }

        await _eventRepository.AddAsync(savingsBoxEvent);

        box.Deactivate();
        await _repository.UpdateAsync(box);
    }

    public async Task<IEnumerable<SavingsBoxEventResponse>> ListSavingsBoxEventsAsync(Guid householdId)
    {
        var events = await _eventRepository.ListByHouseholdAsync(householdId);

        return events.Select(e => new SavingsBoxEventResponse(
            e.Id,
            e.Kind.ToString(),
            e.AccountId,
            e.BoxName,
            e.Reason,
            e.Amount,
            e.DestinationAccountId,
            e.DestinationName,
            e.OccurredAt));
    }

    public async Task UpdateAsync(UpdateAccountCommand command)
    {
        var account = await _repository.GetByIdAsync(command.AccountId);

        if (account is null || account.HouseholdId != command.HouseholdId)
            throw new DomainException("Conta não encontrada.");

        account.UpdateOwnership(
            command.Bank,
            command.Name,
            command.MemberId
        );

        await _repository.UpdateAsync(account);
    }

    public async Task DeleteAsync(DeleteAccountCommand command)
    {
        var account = await _repository.GetByIdAsync(command.AccountId);

        if (account is null || account.HouseholdId != command.HouseholdId)
            throw new DomainException("Conta não encontrada.");

        account.Deactivate();

        await _repository.UpdateAsync(account);
    }
}
