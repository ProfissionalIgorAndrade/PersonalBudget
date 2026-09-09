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

        var passwordHash = _passwordHasher.Hash(command.Password);

        if (!user.CanAuthenticate(passwordHash))
            throw new ApplicationException("Invalid password");

        return user.Id;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest command)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new ApplicationException("Usuário não encontrado.");

        var currentHash = _passwordHasher.Hash(command.CurrentPassword ?? string.Empty);
        if (!user.CanAuthenticate(currentHash))
            throw new ApplicationException("Senha atual incorreta.");

        var newPassword = command.NewPassword ?? string.Empty;

        if (newPassword.Length < MinimumPasswordLength)
            throw new ApplicationException($"A nova senha deve ter ao menos {MinimumPasswordLength} caracteres.");

        var newHash = _passwordHasher.Hash(newPassword);

        if (user.CanAuthenticate(newHash))
            throw new ApplicationException("A nova senha deve ser diferente da atual.");

        user.ChangePassword(newHash);

        // GetByIdAsync is AsNoTracking, so SaveChangesAsync alone would be a
        // silent no-op: the request succeeds and the password never changes.
        await _userRepository.UpdateAsync(user);
    }
}
