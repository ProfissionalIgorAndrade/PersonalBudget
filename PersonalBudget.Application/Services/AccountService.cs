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
    /// Grava o movimento da caixinha como lançamento.
    ///
    /// Sem isto o saldo mudava e nada registrava quando, quanto ou em que
    /// sentido — não havia extrato nem como desenhar evolução.
    ///
    /// PaymentMethod.Savings mantém o lançamento fora dos totais de receita e
    /// despesa, pelo mesmo caminho que já exclui transferência.
    /// </summary>
    private async Task RecordSavingsMovementAsync(Account box, decimal amount, bool isDeposit)
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
            initialStatus: TransactionStatus.Completed);

        await _transactionRepository.AddAsync(tx);
    }

    public async Task<Guid> CreateAsync(CreateAccountCommand command)
    {
        var account = Account.Create(
            command.UserId,
            command.HouseholdId,
            command.Bank,
            new BankAgency(command.Agency),
            new BankAccountNumber(command.AccountNumber),
            new Money(command.InitialBalance),
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

        return accounts.Select(a =>
        {
            string? memberName = a.MemberProfileId.HasValue && profileMap.TryGetValue(a.MemberProfileId.Value, out var n) ? n : null;
            // Caixinha se apresenta pelo próprio nome; o banco e a agência são
            // da conta pai e repeti-los não distingue uma caixinha da outra.
            var displayName = a.Kind == AccountKind.Savings
                ? (a.Name ?? "Caixinha")
                : $"{a.Bank} - {a.Agency.Value}";
            if (memberName is not null && a.Kind != AccountKind.Savings)
                displayName += $" - {memberName}";
            return new AccountResponse(
                a.Id,
                a.Bank.ToString(),
                a.Agency.Value,
                a.Number.Value,
                a.Balance.Amount,
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
        var totalBalance = active.Sum(a => a.Balance.Amount);
        var items = active
            .Select(a =>
            {
                var name = $"{a.Bank} - {a.Agency.Value}";
                if (a.MemberProfileId.HasValue && profileMap.TryGetValue(a.MemberProfileId.Value, out var memberName))
                    name += $" - {memberName}";
                return new AccountSummaryItem(a.Id, name, a.Bank.ToString(), a.Balance.Amount);
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

        box.DepositToSavingsBox(new Money(command.Amount));
        await _repository.UpdateAsync(box);
        await RecordSavingsMovementAsync(box, command.Amount, isDeposit: true);
    }

    public async Task WithdrawFromSavingsBoxAsync(WithdrawFromSavingsBoxCommand command)
    {
        var box = await _repository.GetByIdAsync(command.AccountId)
            ?? throw new DomainException("Caixinha não encontrada.");

        if (box.HouseholdId != command.HouseholdId)
            throw new DomainException("Caixinha não pertence a este lar.");

        box.WithdrawFromSavingsBox(new Money(command.Amount));
        await _repository.UpdateAsync(box);
        await RecordSavingsMovementAsync(box, command.Amount, isDeposit: false);
    }

    public async Task UpdateAsync(UpdateAccountCommand command)
    {
        var account = await _repository.GetByIdAsync(command.AccountId);

        if (account is null || account.HouseholdId != command.HouseholdId)
            throw new DomainException("Conta não encontrada.");

        account.UpdateBankInfo(
            command.Bank,
            new BankAgency(command.Agency),
            new BankAccountNumber(command.AccountNumber),
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

        // Desativar a conta não desativa mais os cartões dela. Antes, excluir
        // uma conta sem lançamentos fazia os cartões associados sumirem da
        // interface, e não havia como recuperá-los - a fatura e o histórico
        // continuavam no banco, invisíveis.
        //
        // O cartão permanece ativo apontando para uma conta inativa, e a conta
        // base pode ser trocada pela edição do cartão.
    }
}
