using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PersonalBudget.Integration.Tests.Tests;

public class RefreshTokenTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private const string Password = "Test@12345";

    private readonly HttpClient _client = factory.CreateClient();

    private record Session(string Email, Guid UserId, string Token, string RefreshToken);

    private async Task<Session> SignUpAsync()
    {
        var email = $"refresh_{Guid.NewGuid():N}@test.com";

        var response = await _client.PostAsJsonAsync("/api/authentication/signin", new
        {
            name = "Refresh User",
            email,
            password = Password
        });
        response.EnsureSuccessStatusCode();

        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        return new Session(
            email,
            data.GetProperty("userId").GetGuid(),
            data.GetProperty("token").GetString()!,
            data.GetProperty("refreshToken").GetString()!);
    }

    private Task<HttpResponseMessage> RefreshAsync(string? refreshToken)
        => _client.PostAsJsonAsync("/api/authentication/refresh", new { refreshToken });

    private static async Task<(string Token, string RefreshToken)> ReadTokensAsync(HttpResponseMessage response)
    {
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        return (data.GetProperty("token").GetString()!, data.GetProperty("refreshToken").GetString()!);
    }

    [Fact]
    public async Task SignIn_ReturnsRefreshToken()
    {
        var session = await SignUpAsync();

        session.RefreshToken.Should().NotBeNullOrWhiteSpace();
        session.RefreshToken.Should().NotBe(session.Token);
    }

    [Fact]
    public async Task Login_ReturnsRefreshToken()
    {
        var session = await SignUpAsync();

        var response = await _client.PostAsJsonAsync("/api/authentication/login", new
        {
            email = session.Email,
            password = Password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (_, refreshToken) = await ReadTokensAsync(response);
        refreshToken.Should().NotBeNullOrWhiteSpace();
        refreshToken.Should().NotBe(session.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewPairThatWorks()
    {
        var session = await SignUpAsync();

        var response = await RefreshAsync(session.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (token, refreshToken) = await ReadTokensAsync(response);
        refreshToken.Should().NotBe(session.RefreshToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/households");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var authorized = await _client.SendAsync(request);
        authorized.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_StoresOnlyTheHashOfTheToken()
    {
        var session = await SignUpAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.UserId == session.UserId);

        stored.TokenHash.Should().NotBe(session.RefreshToken);
        stored.TokenHash.Should().Be(RefreshTokenService.HashToken(session.RefreshToken));
    }

    [Fact]
    public async Task Refresh_ReusingARotatedToken_IsRejectedAndRevokesTheChain()
    {
        var session = await SignUpAsync();

        var first = await RefreshAsync(session.RefreshToken);
        var (_, rotated) = await ReadTokensAsync(first);

        var reuse = await RefreshAsync(session.RefreshToken);
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await reuse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeFalse();

        // The reuse is treated as a leak, so the newest token dies with it.
        var afterReuse = await RefreshAsync(rotated);
        afterReuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("not-a-real-token")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized(string? refreshToken)
    {
        var response = await RefreshAsync(refreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        var session = await SignUpAsync();
        var rawToken = $"expired-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.RefreshTokens.Add(RefreshToken.Create(
                session.UserId,
                RefreshTokenService.HashToken(rawToken),
                now.AddDays(-31),
                now.AddMinutes(-1)));
            await db.SaveChangesAsync();
        }

        var response = await RefreshAsync(rawToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var session = await SignUpAsync();

        var logout = await _client.PostAsJsonAsync("/api/authentication/logout", new { refreshToken = session.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await RefreshAsync(session.RefreshToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_RevokesEveryRefreshTokenOfTheUser()
    {
        var session = await SignUpAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/change-password")
        {
            Content = JsonContent.Create(new { currentPassword = Password, newPassword = "Changed@6789" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var change = await _client.SendAsync(request);
        change.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await RefreshAsync(session.RefreshToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
