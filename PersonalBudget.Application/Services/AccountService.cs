public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;

    public AccountService(IAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> CreateAsync(CreateAccountCommand command)
    {
        var account = Account.Create(
            command.UserId,
            command.HouseholdId,
            command.Bank,
            new BankAgency(command.Agency),
            new BankAccountNumber(command.AccountNumber),
            new Money(command.InitialBalance)
        );

        await _repository.AddAsync(account);
        return account.Id;
    }

    public async Task<IEnumerable<Account>> GetByHouseholdAsync(Guid householdId)
    {
        return await _repository.GetByHouseholdIdAsync(householdId);
    }

    public async Task UpdateAsync(UpdateAccountCommand command)
    {
        var account = await _repository.GetByIdAsync(command.AccountId);

        if (account is null || account.HouseholdId != command.HouseholdId)
            throw new DomainException("Conta não encontrada.");

        account.UpdateBankInfo(
            command.Bank,
            new BankAgency(command.Agency),
            new BankAccountNumber(command.AccountNumber)
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
