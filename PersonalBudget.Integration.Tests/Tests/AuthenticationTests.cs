using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
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
}
