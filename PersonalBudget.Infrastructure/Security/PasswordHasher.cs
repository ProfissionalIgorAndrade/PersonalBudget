using System.Security.Cryptography;
using System.Text;

public class PasswordHasher : IPasswordHasher
{
    public PasswordHash Hash(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("Password cannot be empty.");

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(plainPassword);
        var hashBytes = sha256.ComputeHash(bytes);

        var hash = Convert.ToBase64String(hashBytes);

        return new PasswordHash(hash);
    }
}