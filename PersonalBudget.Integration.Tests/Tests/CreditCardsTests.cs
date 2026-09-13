using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// Testa os endpoints de cartão de crédito — incluindo a coluna member_id
/// que estava faltando em produção e causava o 42703.
/// </summary>
public class CreditCardsTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/credit-cards");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAuth_Returns200()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var response = await _client.GetAsync("/api/credit-cards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsArray()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var response = await _client.GetAsync("/api/credit-cards");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }
}
