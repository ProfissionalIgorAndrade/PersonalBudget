
namespace PersonalBudget.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Guid> CreateAccountAsync(CreateAccountCommand request)
    {
        if (request.UserId == Guid.Empty)
            throw new ApplicationException("UserId is required.");

        var balance = new Money(request.Balance);
        var account = new Account(request.Name, balance, request.UserId);

        await _accountRepository.AddAsync(account);

        return account.Id;
    }

}