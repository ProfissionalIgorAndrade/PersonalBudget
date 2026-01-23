public interface IPasswordHasher
{
    PasswordHash Hash(string plainPassword);
}
