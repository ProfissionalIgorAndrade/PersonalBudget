using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PersonalBudget.Api.Security;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

public class JwtKeyTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    // The key that used to be committed in appsettings.json.
    private const string FormerlyCommittedKey = "THIS_IS_A_SUPER_SECRET_KEY_WITH_AT_LEAST_32_CHARS";

    private static string TokenSignedWith(string key)
        => new JwtTokenGenerator(Options.Create(new JwtSettings
        {
            Key = key,
            Issuer = "PersonalBudget",
            Audience = "PersonalBudget.Web",
            ExpiresInMinutes = 60
        })).Generate(Guid.NewGuid());

    private static async Task<HttpStatusCode> GetHouseholdsAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/households");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (await client.SendAsync(request)).StatusCode;
    }

    [Fact]
    public async Task Token_IssuedByTheApi_IsAccepted()
    {
        var client = factory.CreateClient();
        var token = await AuthHelper.SignInAsync(client);

        (await GetHouseholdsAsync(client, token)).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Token_SignedWithTheFormerlyCommittedKey_IsRejected()
    {
        var client = factory.CreateClient();

        var status = await GetHouseholdsAsync(client, TokenSignedWith(FormerlyCommittedKey));

        status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    public void Startup_WithoutAStrongKey_Fails(string key)
    {
        var broken = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = key
                })));

        var act = () => broken.CreateClient();

        act.Should().Throw<Exception>()
            .Which.ToString().Should().Contain("Jwt:Key");
    }
}
