using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// Testa o endpoint que causou o erro 42703 em produção:
/// GET /api/dashboard/summary retornava erro por conta de member_id ausente no banco.
/// </summary>
public class DashboardTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetSummary_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/dashboard/summary?month=9&year=2026");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_WithAuth_Returns200()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var response = await _client.GetAsync("/api/dashboard/summary?month=9&year=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSummary_WithAuth_ReturnsExpectedShape()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var response = await _client.GetAsync("/api/dashboard/summary?month=9&year=2026");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        data.TryGetProperty("totalIncome",  out _).Should().BeTrue();
        data.TryGetProperty("totalExpense", out _).Should().BeTrue();
        data.TryGetProperty("balance",      out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetTrends_WithAuth_Returns200()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var response = await _client.GetAsync("/api/dashboard/trends?months=3&endMonth=9&endYear=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
