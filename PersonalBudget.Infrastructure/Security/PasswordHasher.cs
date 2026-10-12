using System.Security.Cryptography;
using System.Text;

public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;
    private const string BCryptPrefix = "$2";

    public PasswordHash Hash(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("A senha não pode estar vazia.");

        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);

        return new PasswordHash(hash);
    }

    public bool Verify(string plainPassword, PasswordHash hash)
    {
        if (string.IsNullOrEmpty(plainPassword))
            return false;

        if (IsBCrypt(hash))
            return BCrypt.Net.BCrypt.Verify(plainPassword, hash.Value);

        // Hashes created before BCrypt are unsalted SHA-256 in Base64. They stay
        // valid until the owner logs in, when UserService upgrades them.
        var legacy = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword)));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(legacy),
            Encoding.UTF8.GetBytes(hash.Value));
    }

    public bool NeedsRehash(PasswordHash hash)
        => !IsBCrypt(hash) || BCrypt.Net.BCrypt.PasswordNeedsRehash(hash.Value, WorkFactor);

    private static bool IsBCrypt(PasswordHash hash)
        => hash.Value.StartsWith(BCryptPrefix, StringComparison.Ordinal);
}
