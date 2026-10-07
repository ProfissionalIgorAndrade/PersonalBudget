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
    /// <summary>Exclui (desativa) a caixinha, movendo o saldo para outra caixinha e registrando o evento.</summary>
    Task DeleteSavingsBoxAsync(DeleteSavingsBoxCommand command);
    /// <summary>Eventos de criação e exclusão de caixinhas do lar, do mais recente ao mais antigo.</summary>
    Task<IEnumerable<SavingsBoxEventResponse>> ListSavingsBoxEventsAsync(Guid householdId);
    Task UpdateAsync(UpdateAccountCommand command);
    Task DeleteAsync(DeleteAccountCommand command);
}
