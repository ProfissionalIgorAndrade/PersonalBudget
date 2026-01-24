public interface IUserService
{
    public Task<Guid> CreateUserAsync(SigninRequest command);
    public Task<Guid> AuthenticationUserAsync(LoginUserRequest command);
}