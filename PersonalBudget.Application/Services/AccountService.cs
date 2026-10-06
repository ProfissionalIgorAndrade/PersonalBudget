using PersonalBudget.Application.DTOs.Account;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IHouseholdMemberProfileRepository _profileRepository;
    private readonly ITransactionRepository _transactionRepository;

    public AccountService(
        IAccountRepository repository,
        ICreditCardRepository creditCardRepository,
        IHouseholdMemberProfileRepository profileRepository,
        ITransactionRepository transactionRepository)
    {
        _repository = repository;
        _creditCardRepository = creditCardRepository;
        _profileRepository = profileRepository;
        _transactionRepository = transactionRepository;
    }

    /// <summary>
    /// Grava o movimento da caixinha como lançamento para manter extrato completo.
    /// PaymentMethod.Savings mantém o lançamento fora dos totais de receita e despesa.
    /// </summary>
    private async Task RecordSavingsMovementAsync(Account box, decimal amount, bool isDeposit, string? reason)
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
            description: isDeposit ? $"Depósito em {box.Name}" : $"Resgate de {box.Name}",
            observations: string.IsNullOrWhiteSpace(reason) ? null : reason.Trim());

        await _transactionRepository.AddAsync(tx);
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
        return box.Id;
    }

    public async Task RenameSavingsBoxAsync(RenameSavingsBoxCommand command)
    {
        var box = await _repository.GetByIdAsync(command.AccountId)
            ?? throw new DomainException("Caixinha não encontrada.");

        if (box.HouseholdId != command.HouseholdId)
            throw new DomainException("Caixinha não pertence a este lar.");

        box.RenameSavingsBox(command.Name);
        await _repository.UpdateAsync(box);
    }

    public async Task SetSavingsGoalAsync(SetSavingsGoalCommand command)
    {
        var box = await _repository.GetByIdAsync(command.AccountId)
            ?? throw new DomainException("Caixinha não encontrada.");

        if (box.HouseholdId != command.HouseholdId)
            throw new DomainException("Caixinha não pertence a este lar.");

        box.SetSavingsGoal(command.Goal);
        await _repository.UpdateAsync(box);
    }

    public async Task DepositToSavingsBoxAsync(DepositToSavingsBoxCommand command)
    {
        var box = await _repository.GetByIdAsync(command.AccountId)
            ?? throw new DomainException("Caixinha não encontrada.");

        if (box.HouseholdId != command.HouseholdId)
            throw new DomainException("Caixinha não pertence a este lar.");

        if (box.Kind != AccountKind.Savings)
            throw new DomainException("Esta operação é exclusiva para caixinhas.");

        await RecordSavingsMovementAsync(box, command.Amount, isDeposit: true, command.Reason);
    }

    public async Task WithdrawFromSavingsBoxAsync(WithdrawFromSavingsBoxCommand command)
    {
        var box = await _repository.GetByIdAsync(command.AccountId)
            ?? throw new DomainException("Caixinha não encontrada.");

        if (box.HouseholdId != command.HouseholdId)
            throw new DomainException("Caixinha não pertence a este lar.");

        if (box.Kind != AccountKind.Savings)
            throw new DomainException("Esta operação é exclusiva para caixinhas.");

        await RecordSavingsMovementAsync(box, command.Amount, isDeposit: false, command.Reason);
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
