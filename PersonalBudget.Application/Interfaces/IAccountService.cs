using PersonalBudget.Application.DTOs.Account;

public interface IAccountService
{
    Task<Guid> CreateAsync(CreateAccountCommand command);
    Task<IEnumerable<Account>> GetByHouseholdAsync(Guid householdId);
    Task<AccountsSummaryResponse> GetSummaryAsync(Guid householdId);
    Task UpdateAsync(UpdateAccountCommand command);
    Task DeleteAsync(DeleteAccountCommand command);
}
