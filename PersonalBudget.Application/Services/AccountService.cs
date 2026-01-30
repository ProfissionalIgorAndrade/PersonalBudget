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
            command.Bank,
            new BankAgency(command.Agency),
            new BankAccountNumber(command.AccountNumber),
            new Money(command.InitialBalance)
        );

        await _repository.AddAsync(account);
        return account.Id;
    }

    public async Task<IEnumerable<Account>> GetByUserAsync(Guid userId)
    {
        return await _repository.GetByUserIdAsync(userId);
    }

    public async Task UpdateAsync(UpdateAccountCommand command)
    {
        var account = await _repository.GetByIdAsync(command.AccountId);

        if (account is null || account.UserId != command.UserId)
            throw new DomainException("Account not found.");

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

        if (account is null || account.UserId != command.UserId)
            throw new DomainException("Account not found.");

        account.Deactivate();

        await _repository.UpdateAsync(account);
    }
}
