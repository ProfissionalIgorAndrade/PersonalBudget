namespace PersonalBudget.Application.Services;
public class AccountService
{
    private static readonly List<Account> _accounts = new();

    public IEnumerable<Account> GetAllAccounts() => _accounts;

    public Account CreateAccount(string name, decimal initialBalance)
    {
        var account = new Account(name, new Money(initialBalance));
        _accounts.Add(account);
        return account;
    }

    public Account? GetById(Guid id) => _accounts.FirstOrDefault(a => a.Id == id);
}