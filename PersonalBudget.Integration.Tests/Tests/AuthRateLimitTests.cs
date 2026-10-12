using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace PersonalBudget.Integration.Tests.Tests;

public class AuthRateLimitTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private const int PermitLimit = 3;

    private HttpClient CreateLimitedClient() => factory
        .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Auth:PermitLimit"]   = PermitLimit.ToString(),
                ["RateLimiting:Auth:WindowSeconds"] = "60"
            })))
        .CreateClient();

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string clientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/login")
        {
            Content = JsonContent.Create(new
            {
                email    = $"nobody_{Guid.NewGuid():N}@test.com",
                password = "WrongPassword!"
            })
        };
        request.Headers.Add("Fly-Client-IP", clientIp);

        return client.SendAsync(request);
    }

    [Fact]
    public async Task Login_BeyondTheLimit_ReturnsTooManyRequests()
    {
        var client = CreateLimitedClient();

        for (var attempt = 0; attempt < PermitLimit; attempt++)
        {
            var allowed = await LoginAsync(client, "203.0.113.10");
            allowed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var rejected = await LoginAsync(client, "203.0.113.10");

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.Contains("Retry-After").Should().BeTrue();

        var body = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_FromAnotherClient_IsNotAffectedByTheFirstOne()
    {
        var client = CreateLimitedClient();

        for (var attempt = 0; attempt <= PermitLimit; attempt++)
            await LoginAsync(client, "203.0.113.20");

        var other = await LoginAsync(client, "203.0.113.21");

        other.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
