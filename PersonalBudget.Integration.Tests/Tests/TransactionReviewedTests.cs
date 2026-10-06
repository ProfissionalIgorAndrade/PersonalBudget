using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// Testa a marcação de lançamentos como "revisado": por item
/// (PATCH api/transactions/{id}/reviewed) e em lote por fatura
/// (PATCH api/credit-cards/{cardId}/statement/{statementId}/reviewed).
/// Também exercita a migration que cria a coluna transactions.reviewed.
/// </summary>
public class TransactionReviewedTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private const int Month = 10;
    private const int Year = 2026;

    private sealed record Scenario(
        HttpClient Client,
        Guid AccountId,
        Guid CardId,
        Guid CategoryId,
        Guid ProfileId);

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

        var categoryId = await CreateAndGetIdAsync(client, "/api/categories",
            new { name = "Mercado", type = "Expense" });

        var accountId = await CreateAndGetIdAsync(client, "/api/accounts",
            new { bank = "Nubank", memberId = profileId });

        var cardId = await CreateAndGetIdAsync(client, "/api/credit-cards",
            new { accountId, name = "Cartao", limit = 5000m, closingDay = 25, dueDay = 5, memberId = profileId });

        return new Scenario(client, accountId, cardId, categoryId, profileId);
    }

    private static async Task<JsonElement> GetDataAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data");
    }

    private static async Task<Guid> CreateAndGetIdAsync(HttpClient client, string url, object payload)
    {
        var response = await client.PostAsJsonAsync(url, payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"POST {url} returned: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        return (data.TryGetProperty("id", out var id) ? id : data.GetProperty("transactionId")).GetGuid();
    }

    private static Task<Guid> CreateAccountTransactionAsync(Scenario s, string description) =>
        CreateAndGetIdAsync(s.Client, "/api/transactions", new
        {
            accountId = s.AccountId,
            categoryId = s.CategoryId,
            type = "Expense",
            frequency = "Variable",
            paymentMethod = "Account",
            amount = 10m,
            date = "05/10/2026",
            description,
            attributionProfileId = s.ProfileId
        });

    private static Task<Guid> CreateCardTransactionAsync(Scenario s, string description) =>
        CreateAndGetIdAsync(s.Client, "/api/transactions", new
        {
            accountId = s.AccountId,
            categoryId = s.CategoryId,
            creditCardId = s.CardId,
            type = "Expense",
            frequency = "Variable",
            paymentMethod = "CreditCard",
            amount = 25m,
            date = "05/10/2026",
            description,
            statementMonth = Month,
            statementYear = Year,
            attributionProfileId = s.ProfileId
        });

    private static Task<HttpResponseMessage> PatchReviewedAsync(HttpClient client, string url, bool reviewed) =>
        client.PatchAsJsonAsync(url, new { reviewed });

    private static async Task<JsonElement> GetStatementAsync(Scenario s)
    {
        var response = await s.Client.GetAsync($"/api/credit-cards/{s.CardId}/statement?month={Month}&year={Year}");
        return await GetDataAsync(response);
    }

    private static async Task<bool> GetAccountTxReviewedAsync(Scenario s, Guid transactionId)
    {
        var response = await s.Client.GetAsync($"/api/accounts/{s.AccountId}/transactions?month={Month}&year={Year}");
        var data = await GetDataAsync(response);
        return data.EnumerateArray()
            .First(t => t.GetProperty("id").GetGuid() == transactionId)
            .GetProperty("reviewed").GetBoolean();
    }

    [Fact]
    public async Task SetReviewed_OnAccountTransaction_TogglesAndPersistsInListing()
    {
        var s = await CreateScenarioAsync();
        var id = await CreateAccountTransactionAsync(s, "Compra conta");

        (await GetAccountTxReviewedAsync(s, id)).Should().BeFalse();

        var on = await PatchReviewedAsync(s.Client, $"/api/transactions/{id}/reviewed", true);
        on.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAccountTxReviewedAsync(s, id)).Should().BeTrue();

        var off = await PatchReviewedAsync(s.Client, $"/api/transactions/{id}/reviewed", false);
        off.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAccountTxReviewedAsync(s, id)).Should().BeFalse();
    }

    [Fact]
    public async Task SetReviewed_OnStatementTransaction_AppearsInStatementListing()
    {
        var s = await CreateScenarioAsync();
        var id = await CreateCardTransactionAsync(s, "Compra cartao");

        var response = await PatchReviewedAsync(s.Client, $"/api/transactions/{id}/reviewed", true);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var statement = await GetStatementAsync(s);
        statement.GetProperty("transactions").EnumerateArray()
            .Single(t => t.GetProperty("id").GetGuid() == id)
            .GetProperty("reviewed").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SetStatementReviewed_MarksAndUnmarksAllItems()
    {
        var s = await CreateScenarioAsync();
        await CreateCardTransactionAsync(s, "Compra 1");
        await CreateCardTransactionAsync(s, "Compra 2");
        await CreateCardTransactionAsync(s, "Compra 3");

        var statement = await GetStatementAsync(s);
        var statementId = statement.GetProperty("statementId").GetGuid();
        var url = $"/api/credit-cards/{s.CardId}/statement/{statementId}/reviewed";

        var on = await PatchReviewedAsync(s.Client, url, true);
        on.StatusCode.Should().Be(HttpStatusCode.OK);

        var marked = (await GetStatementAsync(s)).GetProperty("transactions").EnumerateArray().ToList();
        marked.Should().HaveCount(3);
        marked.Should().OnlyContain(t => t.GetProperty("reviewed").GetBoolean());

        var off = await PatchReviewedAsync(s.Client, url, false);
        off.StatusCode.Should().Be(HttpStatusCode.OK);

        var unmarked = (await GetStatementAsync(s)).GetProperty("transactions").EnumerateArray().ToList();
        unmarked.Should().OnlyContain(t => !t.GetProperty("reviewed").GetBoolean());
    }

    [Fact]
    public async Task SetReviewed_FromAnotherHousehold_ReturnsBadRequestAndDoesNotChange()
    {
        var owner = await CreateScenarioAsync();
        var id = await CreateAccountTransactionAsync(owner, "Compra privada");

        var intruder = factory.CreateClient();
        intruder.SetBearer(await AuthHelper.SignInAsync(intruder));

        var response = await PatchReviewedAsync(intruder, $"/api/transactions/{id}/reviewed", true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetAccountTxReviewedAsync(owner, id)).Should().BeFalse();
    }

    [Fact]
    public async Task SetStatementReviewed_FromAnotherHousehold_ReturnsBadRequestAndDoesNotChange()
    {
        var owner = await CreateScenarioAsync();
        await CreateCardTransactionAsync(owner, "Compra privada");
        var statementId = (await GetStatementAsync(owner)).GetProperty("statementId").GetGuid();

        var intruder = factory.CreateClient();
        intruder.SetBearer(await AuthHelper.SignInAsync(intruder));

        var response = await PatchReviewedAsync(
            intruder, $"/api/credit-cards/{owner.CardId}/statement/{statementId}/reviewed", true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetStatementAsync(owner)).GetProperty("transactions").EnumerateArray()
            .Should().OnlyContain(t => !t.GetProperty("reviewed").GetBoolean());
    }

    [Fact]
    public async Task SetReviewed_WithoutAuth_Returns401()
    {
        var client = factory.CreateClient();

        var response = await PatchReviewedAsync(client, $"/api/transactions/{Guid.NewGuid()}/reviewed", true);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
