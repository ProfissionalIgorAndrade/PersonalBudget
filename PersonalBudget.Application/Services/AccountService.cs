
using PersonalBudget.Domain.Enums;

namespace PersonalBudget.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountUserRepository _accountUserRepository;

    public AccountService(IAccountRepository accountRepository, IAccountUserRepository accountUserRepository)
    {
        _accountRepository = accountRepository;
        _accountUserRepository = accountUserRepository;
    }

    public async Task<Guid> CreateAccountAsync(CreateAccountCommand request)
    {
        if (request.UserId == Guid.Empty)
            throw new ApplicationException("UserId is required.");

        var balance = new Money(request.Balance);
        var account = new Account(request.Name, balance);

        await _accountRepository.AddAsync(account);

        var accountUser = new AccountUser(
            accountId: account.Id,
            userId: request.UserId,
            role: AccountRole.Owner
        );

        await _accountUserRepository.AddAsync(accountUser);

        return account.Id;
    }

    public async Task JoinAccountAsync(JoinAccountCommand request)
    {
        if (request.UserId == Guid.Empty || request.AccountId == Guid.Empty)
            throw new ApplicationException("UserId and AccountId are required.");

        var account = await _accountRepository.GetByIdAsync(request.AccountId);
        if (account == null)
            throw new ApplicationException("Account not found.");

        var alreadyLinked = await _accountUserRepository
            .ExistsAsync(request.AccountId, request.UserId);

        if (alreadyLinked)
            throw new ApplicationException("User already belongs to this account.");

        var accountUser = new AccountUser(
            accountId: request.AccountId,
            userId: request.UserId,
            role: AccountRole.Member
        );

        await _accountUserRepository.AddAsync(accountUser);
    }
}