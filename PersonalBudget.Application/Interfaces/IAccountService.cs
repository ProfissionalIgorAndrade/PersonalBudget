public interface IAccountService
{
    public Task<Guid> CreateAccountAsync(CreateAccountCommand request);
}