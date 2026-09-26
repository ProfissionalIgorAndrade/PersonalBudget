using PersonalBudget.Application.DTOs.Account;

public interface IAccountService
{
    Task<Guid> CreateAsync(CreateAccountCommand command);
    Task<IEnumerable<AccountResponse>> GetByHouseholdAsync(Guid householdId);
    Task<AccountsSummaryResponse> GetSummaryAsync(Guid householdId);
    Task<Guid> CreateSavingsBoxAsync(CreateSavingsBoxCommand command);
    Task RenameSavingsBoxAsync(RenameSavingsBoxCommand command);
    Task SetSavingsGoalAsync(SetSavingsGoalCommand command);
    Task DepositToSavingsBoxAsync(DepositToSavingsBoxCommand command);
    Task WithdrawFromSavingsBoxAsync(WithdrawFromSavingsBoxCommand command);
    Task UpdateAsync(UpdateAccountCommand command);
    Task DeleteAsync(DeleteAccountCommand command);
}
