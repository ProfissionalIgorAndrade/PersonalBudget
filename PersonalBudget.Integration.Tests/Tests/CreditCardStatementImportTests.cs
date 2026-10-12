using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// Testa a importação de CSV para uma fatura (cartão + mês/ano):
/// linhas novas entram na fatura informada e linhas que já existem,
/// por id ou por conteúdo, nunca são duplicadas.
/// </summary>
public class CreditCardStatementImportTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private const int Month = 2;
    private const int Year = 2026;

    private sealed record Scenario(HttpClient Client, Guid ProfileId, Guid CategoryId, Guid CardId);

    private async Task<Scenario> CreateScenarioAsync()
    {
        var client = factory.CreateClient();
        client.SetBearer(await AuthHelper.SignInAsync(client));

        var households = await GetDataAsync(await client.GetAsync("/api/households"));
        var householdId = households[0].GetProperty("id").GetGuid();

        var profiles = await GetDataAsync(await client.GetAsync($"/api/households/{householdId}/profiles"));
        var profileId = profiles.EnumerateArray()
            .First(p => p.GetProperty("userId").ValueKind != JsonValueKind.Null)
            .GetProperty("id").GetGuid();

        var categoryId = await PostAndGetIdAsync(client, "/api/categories",
            new { name = "Mercado", type = "Expense" });
        var cardId = await PostAndGetIdAsync(client, "/api/credit-cards",
            new { name = "Cartao", limit = 5000m, dueDay = 10, memberId = profileId });

        return new Scenario(client, profileId, categoryId, cardId);
    }

    private static async Task<JsonElement> GetDataAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"unexpected response: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data");
    }

    private static async Task<Guid> PostAndGetIdAsync(HttpClient client, string url, object payload)
    {
        var response = await client.PostAsJsonAsync(url, payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"POST {url} returned: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        return (data.TryGetProperty("id", out var id) ? id : data.GetProperty("transactionId")).GetGuid();
    }

    private static Task<Guid> CreateCardPurchaseAsync(
        Scenario s, string description = "Padaria", decimal amount = 25.50m, string date = "2026-01-15") =>
        PostAndGetIdAsync(s.Client, "/api/transactions", new
        {
            categoryId = s.CategoryId,
            creditCardId = s.CardId,
            type = "Expense",
            frequency = "Variable",
            paymentMethod = "CreditCard",
            amount,
            date,
            description,
            statementMonth = Month,
            statementYear = Year,
            attributionProfileId = s.ProfileId
        });

    private static object Row(
        Scenario s, string description = "Padaria", decimal amount = 25.50m,
        string date = "2026-01-15", string? externalId = null, string type = "Expense") => new
        {
            externalId,
            description,
            amount,
            date,
            type,
            categoryId = s.CategoryId,
            attributionProfileId = s.ProfileId,
            observations = (string?)null
        };

    private static async Task<JsonElement> ImportAsync(Scenario s, bool dryRun, params object[] rows) =>
        await GetDataAsync(await s.Client.PostAsJsonAsync(
            $"/api/credit-cards/{s.CardId}/statement/import",
            new { statementMonth = Month, statementYear = Year, dryRun, rows }));

    private static async Task<List<JsonElement>> GetStatementTransactionsAsync(Scenario s)
    {
        var response = await s.Client.GetAsync(
            $"/api/credit-cards/{s.CardId}/statement?month={Month}&year={Year}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return [];
        return (await GetDataAsync(response)).GetProperty("transactions").EnumerateArray().ToList();
    }

    private static string[] Statuses(JsonElement result) =>
        result.GetProperty("rows").EnumerateArray()
            .Select(r => r.GetProperty("status").GetString()!)
            .ToArray();

    [Fact]
    public async Task Import_NewRows_AreCreatedInTheGivenStatement()
    {
        var s = await CreateScenarioAsync();

        var result = await ImportAsync(s, dryRun: false,
            Row(s, "Padaria", 25.50m),
            Row(s, "Estorno loja", 10m, type: "Income"));

        result.GetProperty("created").GetInt32().Should().Be(2);
        result.GetProperty("skipped").GetInt32().Should().Be(0);
        Statuses(result).Should().Equal("New", "New");

        var transactions = await GetStatementTransactionsAsync(s);
        transactions.Select(t => t.GetProperty("description").GetString())
            .Should().BeEquivalentTo("Padaria", "Estorno loja");
    }

    [Fact]
    public async Task Import_SameRowsTwice_DoesNotDuplicate()
    {
        var s = await CreateScenarioAsync();
        var rows = new[] { Row(s, "Padaria", 25.50m), Row(s, "Farmacia", 80m, "2026-01-20") };

        await ImportAsync(s, dryRun: false, rows);
        var second = await ImportAsync(s, dryRun: false, rows);

        second.GetProperty("created").GetInt32().Should().Be(0);
        second.GetProperty("skipped").GetInt32().Should().Be(2);
        Statuses(second).Should().Equal("Existing", "Existing");
        (await GetStatementTransactionsAsync(s)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Import_DryRun_ClassifiesWithoutCreating()
    {
        var s = await CreateScenarioAsync();
        await CreateCardPurchaseAsync(s, "Padaria", 25.50m);

        var result = await ImportAsync(s, dryRun: true,
            Row(s, "Padaria", 25.50m),
            Row(s, "Farmacia", 80m, "2026-01-20"));

        result.GetProperty("created").GetInt32().Should().Be(0);
        Statuses(result).Should().Equal("Existing", "New");
        (await GetStatementTransactionsAsync(s)).Should().HaveCount(1);
    }

    [Fact]
    public async Task Import_RowWithExistingId_IsSkippedEvenIfContentChanged()
    {
        var s = await CreateScenarioAsync();
        var existingId = await CreateCardPurchaseAsync(s, "Padaria", 25.50m);

        var result = await ImportAsync(s, dryRun: false,
            Row(s, "Padaria editada", 99m, externalId: existingId.ToString()));

        Statuses(result).Should().Equal("Existing");
        var transactions = await GetStatementTransactionsAsync(s);
        transactions.Should().ContainSingle()
            .Which.GetProperty("description").GetString().Should().Be("Padaria");
    }

    [Fact]
    public async Task Import_IdFromAnotherHousehold_IsNotTreatedAsExisting()
    {
        var other = await CreateScenarioAsync();
        var foreignId = await CreateCardPurchaseAsync(other, "Compra alheia", 300m);
        var s = await CreateScenarioAsync();

        var result = await ImportAsync(s, dryRun: false,
            Row(s, "Padaria", 25.50m, externalId: foreignId.ToString()));

        result.GetProperty("created").GetInt32().Should().Be(1);
        Statuses(result).Should().Equal("New");
    }

    [Fact]
    public async Task Import_InvalidExternalId_FallsBackToContentMatch()
    {
        var s = await CreateScenarioAsync();
        await CreateCardPurchaseAsync(s, "Padaria", 25.50m);

        var result = await ImportAsync(s, dryRun: false,
            Row(s, "  PADARIA ", 25.50m, externalId: "nao-e-um-guid"));

        Statuses(result).Should().Equal("Existing");
        (await GetStatementTransactionsAsync(s)).Should().HaveCount(1);
    }

    [Fact]
    public async Task Import_TwoIdenticalRowsAgainstOneExisting_CreatesOnlyOne()
    {
        var s = await CreateScenarioAsync();
        await CreateCardPurchaseAsync(s, "Padaria", 25.50m);

        var result = await ImportAsync(s, dryRun: false,
            Row(s, "Padaria", 25.50m),
            Row(s, "Padaria", 25.50m));

        result.GetProperty("created").GetInt32().Should().Be(1);
        result.GetProperty("skipped").GetInt32().Should().Be(1);
        Statuses(result).Should().Equal("Existing", "New");
        (await GetStatementTransactionsAsync(s)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Import_RowWithoutCategory_ReportsErrorAndKeepsImportingTheRest()
    {
        var s = await CreateScenarioAsync();

        var result = await ImportAsync(s, dryRun: false,
            new
            {
                externalId = (string?)null,
                description = "Sem categoria",
                amount = 10m,
                date = "2026-01-15",
                type = "Expense",
                categoryId = (Guid?)null,
                attributionProfileId = s.ProfileId,
                observations = (string?)null
            },
            Row(s, "Padaria", 25.50m));

        result.GetProperty("created").GetInt32().Should().Be(1);
        result.GetProperty("errors").GetArrayLength().Should().Be(1);
        Statuses(result).Should().Equal("Error", "New");
    }

    [Fact]
    public async Task Import_CardFromAnotherHousehold_ReturnsBadRequest()
    {
        var other = await CreateScenarioAsync();
        var s = await CreateScenarioAsync();

        var response = await s.Client.PostAsJsonAsync(
            $"/api/credit-cards/{other.CardId}/statement/import",
            new { statementMonth = Month, statementYear = Year, dryRun = true, rows = new[] { Row(s) } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
