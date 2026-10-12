using PersonalBudget.Application.Interfaces;

public class UserService : IUserService
{
    private const int MinimumPasswordLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHouseholdProvisioningService _householdProvisioning;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IHouseholdProvisioningService householdProvisioning)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _householdProvisioning = householdProvisioning;
    }

    public async Task<Guid> CreateUserAsync(SigninRequest command)
    {
        var email = new Email(command.Email);

        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser != null)
            throw new ApplicationException("Email already registered.");

        var passwordHasher = _passwordHasher.Hash(command.Password);

        var user = new User(command.Name, email, passwordHasher);

        await _userRepository.AddAsync(user);
        await _householdProvisioning.ProvisionNewUserAsync(user.Id, user.Name);

        return user.Id;
    }

    public async Task<Guid> AuthenticationUserAsync(LoginUserRequest command)
    {
        var email = new Email(command.Email);
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
            throw new ApplicationException("User does not exist.");

        if (!user.CanAuthenticate(command.Password, _passwordHasher))
            throw new ApplicationException("Invalid password");

        // The plain password is only available here, so this is the one place
        // a hash made with an older algorithm can be upgraded.
        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.ChangePassword(_passwordHasher.Hash(command.Password));
            await _userRepository.UpdateAsync(user);
        }

        return user.Id;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest command)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new ApplicationException("Usuário não encontrado.");

        if (!user.CanAuthenticate(command.CurrentPassword ?? string.Empty, _passwordHasher))
            throw new ApplicationException("Senha atual incorreta.");

        var newPassword = command.NewPassword ?? string.Empty;

        if (newPassword.Length < MinimumPasswordLength)
            throw new ApplicationException($"A nova senha deve ter ao menos {MinimumPasswordLength} caracteres.");

        if (user.CanAuthenticate(newPassword, _passwordHasher))
            throw new ApplicationException("A nova senha deve ser diferente da atual.");

        user.ChangePassword(_passwordHasher.Hash(newPassword));

        // GetByIdAsync is AsNoTracking, so SaveChangesAsync alone would be a
        // silent no-op: the request succeeds and the password never changes.
        await _userRepository.UpdateAsync(user);
    }
}
