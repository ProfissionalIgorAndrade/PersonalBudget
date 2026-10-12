using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

public class AuthenticationTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SignIn_WithValidData_ReturnsTokenAndUserId()
    {
        var response = await _client.PostAsJsonAsync("/api/authentication/signin", new
        {
            name     = "Igor Andrade",
            email    = $"igor_{Guid.NewGuid():N}@test.com",
            password = "Test@12345"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        data.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetProperty("userId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var email    = $"login_{Guid.NewGuid():N}@test.com";
        var password = "Test@12345";

        // create user first
        await AuthHelper.SignInAsync(_client, email, password);

        var response = await _client.PostAsJsonAsync("/api/authentication/login", new
        {
            email,
            password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("token").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequest()
    {
        var email = $"wrong_{Guid.NewGuid():N}@test.com";
        await AuthHelper.SignInAsync(_client, email, "Test@12345");

        var response = await _client.PostAsJsonAsync("/api/authentication/login", new
        {
            email,
            password = "WrongPassword!"
        });

        // The middleware maps ApplicationException (invalid password) to 400 BadRequest,
        // not 401 Unauthorized — authentication in this API is form-based, not HTTP auth.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithLegacySha256Hash_SucceedsAndUpgradesHash()
    {
        var email    = $"legacy_{Guid.NewGuid():N}@test.com";
        var password = "Test@12345";

        // Users created before BCrypt have an unsalted SHA-256 hash stored.
        var legacyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        var user = new User("Legacy User", new Email(email), new PasswordHash(legacyHash));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var first = await _client.PostAsJsonAsync("/api/authentication/login", new { email, password });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
            stored.PasswordHash.Value.Should().StartWith("$2");
        }

        var second = await _client.PostAsJsonAsync("/api/authentication/login", new { email, password });
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var wrong = await _client.PostAsJsonAsync("/api/authentication/login", new { email, password = "WrongPassword!" });
        wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_ReplacesThePasswordUsedToLogin()
    {
        var email       = $"change_{Guid.NewGuid():N}@test.com";
        var oldPassword = "Test@12345";
        var newPassword = "Changed@6789";

        var token = await AuthHelper.SignInAsync(_client, email, oldPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/change-password")
        {
            Content = JsonContent.Create(new { currentPassword = oldPassword, newPassword })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var change = await _client.SendAsync(request);
        change.StatusCode.Should().Be(HttpStatusCode.OK);

        var withNew = await _client.PostAsJsonAsync("/api/authentication/login", new { email, password = newPassword });
        withNew.StatusCode.Should().Be(HttpStatusCode.OK);

        var withOld = await _client.PostAsJsonAsync("/api/authentication/login", new { email, password = oldPassword });
        withOld.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithSamePassword_ReturnsBadRequest()
    {
        var password = "Test@12345";
        var token = await AuthHelper.SignInAsync(_client, password: password);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/change-password")
        {
            Content = JsonContent.Create(new { currentPassword = password, newPassword = password })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
