using FluentAssertions;

namespace PersonalBudget.Integration.Tests.Tests;

public class CorsTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";

    private readonly HttpClient _client = factory.CreateClient();

    private async Task<HttpResponseMessage> PreflightAsync(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/authentication/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type,x-household-id");

        return await _client.SendAsync(request);
    }

    [Theory]
    [InlineData("https://personal-budget-ui.vercel.app")]
    [InlineData("https://personal-budget-h9aw4oe2l-profissionaligorandrades-projects.vercel.app")]
    [InlineData("https://personal-budget-git-feat-something-profissionaligorandrades-projects.vercel.app")]
    public async Task Preflight_FromAllowedOrigin_ReturnsAllowOriginHeader(string origin)
    {
        var response = await PreflightAsync(origin);

        response.Headers.TryGetValues(AllowOriginHeader, out var values).Should().BeTrue();
        values.Should().ContainSingle().Which.Should().Be(origin);
    }

    [Theory]
    [InlineData("https://evil.example.com")]
    [InlineData("http://personal-budget-ui.vercel.app")]
    [InlineData("https://personal-budget-ui.vercel.app.evil.com")]
    [InlineData("https://personal-budget-x-profissionaligorandrades-projects.vercel.app.evil.com")]
    [InlineData("https://personal-budget-x.evil.com-profissionaligorandrades-projects.vercel.app")]
    [InlineData("https://other-app-profissionaligorandrades-projects.vercel.app")]
    public async Task Preflight_FromUnknownOrigin_OmitsAllowOriginHeader(string origin)
    {
        var response = await PreflightAsync(origin);

        response.Headers.Contains(AllowOriginHeader).Should().BeFalse();
    }
}
