public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
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

        return user.Id;
    }

    public async Task<Guid> AuthenticationUserAsync(AuthenticateUserRequest command)
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
}