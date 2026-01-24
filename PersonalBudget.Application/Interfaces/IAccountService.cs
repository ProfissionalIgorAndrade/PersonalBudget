public interface IAccountService
{
    public Task<Guid> CreateAccountAsync(CreateAccountRequest request);
    public Task JoinAccountAsync(JoinAccountRequest request);
}