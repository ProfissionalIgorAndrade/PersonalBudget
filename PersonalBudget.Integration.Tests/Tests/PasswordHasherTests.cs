using System.Security.Cryptography;
using System.Text;
using FluentAssertions;

namespace PersonalBudget.Integration.Tests.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    private static PasswordHash LegacyHash(string password)
        => new(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password))));

    [Fact]
    public void Hash_ProducesSaltedBCryptHash()
    {
        var first  = _hasher.Hash("Test@12345");
        var second = _hasher.Hash("Test@12345");

        first.Value.Should().StartWith("$2");
        first.Value.Should().NotBe(second.Value);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Test@12345");

        _hasher.Verify("Test@12345", hash).Should().BeTrue();
        _hasher.NeedsRehash(hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("WrongPassword!")]
    [InlineData("")]
    public void Verify_WithWrongPassword_ReturnsFalse(string attempt)
    {
        var hash = _hasher.Hash("Test@12345");

        _hasher.Verify(attempt, hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithLegacySha256Hash_StillAuthenticatesAndAsksForRehash()
    {
        var legacy = LegacyHash("Test@12345");

        _hasher.Verify("Test@12345", legacy).Should().BeTrue();
        _hasher.Verify("WrongPassword!", legacy).Should().BeFalse();
        _hasher.NeedsRehash(legacy).Should().BeTrue();
    }
}
