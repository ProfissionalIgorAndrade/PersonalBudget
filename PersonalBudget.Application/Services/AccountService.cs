using PersonalBudget.Application.DTOs.Account;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IHouseholdMemberProfileRepository _profileRepository;

    public AccountService(
        IAccountRepository repository,
        ICreditCardRepository creditCardRepository,
        IHouseholdMemberProfileRepository profileRepository)
    {
        _repository = repository;
        _creditCardRepository = creditCardRepository;
        _profileRepository = profileRepository;
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
            var displayName = $"{a.Bank} - {a.Agency.Value}";
            if (memberName is not null) displayName += $" - {memberName}";
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
                a.CreatedAt
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
        await _creditCardRepository.DeactivateByAccountIdAsync(command.AccountId);
    }
}
