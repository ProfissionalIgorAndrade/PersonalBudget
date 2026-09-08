public interface IUserService
{
    public Task<Guid> CreateUserAsync(SigninRequest command);
    public Task<Guid> AuthenticationUserAsync(LoginUserRequest command);
    public Task ChangePasswordAsync(Guid userId, ChangePasswordRequest command);
}