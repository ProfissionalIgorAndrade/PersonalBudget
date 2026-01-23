public interface IUserService
{
    public Task<Guid> CreateUserAsync(RegisterUserCommand command);
    public Task<AuthenticationResult> AuthenticationUserAsync(AuthenticateUserCommand command);
}