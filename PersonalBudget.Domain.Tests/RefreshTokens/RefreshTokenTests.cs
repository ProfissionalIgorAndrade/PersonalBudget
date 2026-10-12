using FluentAssertions;

namespace PersonalBudget.Domain.Tests.RefreshTokens;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);

    private static RefreshToken NewToken()
        => RefreshToken.Create(Guid.NewGuid(), "hash", Now, Now.AddDays(30));

    [Fact]
    public void Create_StartsActive()
    {
        var token = NewToken();

        token.IsRevoked.Should().BeFalse();
        token.IsExpired(Now).Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutUser_Throws()
    {
        var act = () => RefreshToken.Create(Guid.Empty, "hash", Now, Now.AddDays(30));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithoutHash_Throws()
    {
        var act = () => RefreshToken.Create(Guid.NewGuid(), " ", Now, Now.AddDays(30));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsExpired_AtOrAfterExpiration_ReturnsTrue()
    {
        var token = NewToken();

        token.IsExpired(Now.AddDays(30)).Should().BeTrue();
        token.IsExpired(Now.AddDays(31)).Should().BeTrue();
        token.IsExpired(Now.AddDays(30).AddSeconds(-1)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_RecordsWhenAndWhatReplacedIt()
    {
        var token = NewToken();
        var replacement = Guid.NewGuid();

        token.Revoke(Now.AddHours(1), replacement);

        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().Be(Now.AddHours(1));
        token.ReplacedByTokenId.Should().Be(replacement);
    }

    [Fact]
    public void Revoke_Twice_KeepsTheFirstRevocation()
    {
        var token = NewToken();
        var replacement = Guid.NewGuid();

        token.Revoke(Now.AddHours(1), replacement);
        token.Revoke(Now.AddHours(2));

        token.RevokedAt.Should().Be(Now.AddHours(1));
        token.ReplacedByTokenId.Should().Be(replacement);
    }
}
