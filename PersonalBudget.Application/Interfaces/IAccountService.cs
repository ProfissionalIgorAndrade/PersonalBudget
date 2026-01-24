public interface IAccountService
{
    public Task<Guid> CreateAccountAsync(CreateAccountCommand request);
    public Task JoinAccountAsync(JoinAccountCommand request);
}