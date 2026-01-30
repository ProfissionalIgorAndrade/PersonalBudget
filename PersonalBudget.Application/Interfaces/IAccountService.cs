public interface IAccountService
{
    Task<Guid> CreateAsync(CreateAccountCommand command);
    Task<IEnumerable<Account>> GetByUserAsync(Guid userId);
    Task UpdateAsync(UpdateAccountCommand command);
    Task DeleteAsync(DeleteAccountCommand command);
}