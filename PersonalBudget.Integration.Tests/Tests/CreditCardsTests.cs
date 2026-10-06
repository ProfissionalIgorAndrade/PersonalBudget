using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// Testa os endpoints de cartão de crédito — incluindo a coluna member_id
/// que estava faltando em produção e causava o 42703.
/// Cobre o cartão sem conta de débito e sem dia de fechamento, a fatura sem status
/// (identificada por cartão, mês e ano) e a compra de cartão sem conta.
/// Também exercita a migration que remove esses campos e cria o índice único da fatura.
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

    private const int Month = 2;
    private const int Year = 2026;

    private sealed record Scenario(HttpClient Client, Guid ProfileId, Guid CategoryId);

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

        return new Scenario(client, profileId, categoryId);
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

    private static Task<Guid> CreateCardAsync(Scenario s, int dueDay = 10) =>
        PostAndGetIdAsync(s.Client, "/api/credit-cards",
            new { name = "Cartao", limit = 5000m, dueDay, memberId = s.ProfileId });

    private static Task<Guid> CreateCardPurchaseAsync(
        Scenario s, Guid cardId, decimal amount, int month = Month, int year = Year) =>
        PostAndGetIdAsync(s.Client, "/api/transactions", new
        {
            categoryId = s.CategoryId,
            creditCardId = cardId,
            type = "Expense",
            frequency = "Variable",
            paymentMethod = "CreditCard",
            amount,
            date = "05/02/2026",
            description = "Compra cartao",
            statementMonth = month,
            statementYear = year,
            attributionProfileId = s.ProfileId
        });

    private static async Task<JsonElement> GetStatementAsync(
        Scenario s, Guid cardId, int month = Month, int year = Year, string extraQuery = "")
    {
        var response = await s.Client.GetAsync(
            $"/api/credit-cards/{cardId}/statement?month={month}&year={year}{extraQuery}");
        return await GetDataAsync(response);
    }

    [Fact]
    public async Task Create_WithOnlyNameLimitAndDueDay_ReturnsCreated()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Client.PostAsJsonAsync("/api/credit-cards",
            new { name = "Visa", limit = 3000m, dueDay = 15 });

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_WithOutOfRangeDueDay_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Client.PostAsJsonAsync("/api/credit-cards",
            new { name = "Visa", limit = 3000m, dueDay = 32 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_ExposesDueDayButNotAccountOrClosingDay()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s, dueDay: 12);

        var cards = await GetDataAsync(await s.Client.GetAsync("/api/credit-cards"));
        var card = cards.EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == cardId);

        card.GetProperty("dueDay").GetInt32().Should().Be(12);
        card.TryGetProperty("accountId", out _).Should().BeFalse();
        card.TryGetProperty("closingDay", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Update_ChangesDueDayWithoutAccountOrClosingDay()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s, dueDay: 10);

        var response = await s.Client.PutAsJsonAsync($"/api/credit-cards/{cardId}",
            new { name = "Cartao novo", limit = 7000m, dueDay = 20 });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var cards = await GetDataAsync(await s.Client.GetAsync("/api/credit-cards"));
        var card = cards.EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == cardId);
        card.GetProperty("name").GetString().Should().Be("Cartao novo");
        card.GetProperty("dueDay").GetInt32().Should().Be(20);
    }

    [Fact]
    public async Task Statement_HasComputedDueDateAndNoStatusOrClosingFields()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s, dueDay: 31);
        await CreateCardPurchaseAsync(s, cardId, 40m);

        var statement = await GetStatementAsync(s, cardId);

        foreach (var removed in new[] { "status", "closingDate", "periodStart", "periodEnd" })
            statement.TryGetProperty(removed, out _).Should().BeFalse($"'{removed}' was removed from the statement");

        // Dia 31 em fevereiro de 2026 (28 dias) vence dia 28.
        statement.GetProperty("dueDate").GetString().Should().StartWith("2026-02-28");
        statement.GetProperty("totalAmount").GetDecimal().Should().Be(40m);
    }

    [Fact]
    public async Task PagedStatement_HasNoStatusOrClosingFields()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s, dueDay: 10);
        await CreateCardPurchaseAsync(s, cardId, 40m);

        var statement = await GetStatementAsync(s, cardId, extraQuery: "&page=1");

        foreach (var removed in new[] { "status", "closingDate", "periodStart", "periodEnd" })
            statement.TryGetProperty(removed, out _).Should().BeFalse($"'{removed}' was removed from the statement");
        statement.GetProperty("dueDate").GetString().Should().StartWith("2026-02-10");
        statement.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CardPurchase_HasNullAccountIdAndDoesNotChangeAccountBalance()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s);
        var accountId = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", memberId = s.ProfileId });
        await PostAndGetIdAsync(s.Client, "/api/transactions", new
        {
            accountId,
            categoryId = s.CategoryId,
            type = "Income",
            frequency = "Variable",
            paymentMethod = "Account",
            amount = 100m,
            date = "05/02/2026",
            description = "Entrada",
            attributionProfileId = s.ProfileId
        });

        async Task<decimal> BalanceAsync()
        {
            var accounts = await GetDataAsync(await s.Client.GetAsync("/api/accounts"));
            return accounts.EnumerateArray()
                .Single(a => a.GetProperty("id").GetGuid() == accountId)
                .GetProperty("balance").GetDecimal();
        }

        (await BalanceAsync()).Should().Be(100m);

        var purchaseId = await CreateCardPurchaseAsync(s, cardId, 25m);

        (await BalanceAsync()).Should().Be(100m);

        var all = await GetDataAsync(await s.Client.GetAsync("/api/transactions"));
        var row = all.EnumerateArray().Single(t => t.GetProperty("id").GetGuid() == purchaseId);
        row.GetProperty("accountId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task TwoPurchasesInSameMonth_UseOneStatement()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s);

        await CreateCardPurchaseAsync(s, cardId, 30m);
        var firstStatement = await GetStatementAsync(s, cardId);
        await CreateCardPurchaseAsync(s, cardId, 20m);
        var secondStatement = await GetStatementAsync(s, cardId);

        secondStatement.GetProperty("statementId").GetGuid()
            .Should().Be(firstStatement.GetProperty("statementId").GetGuid());
        secondStatement.GetProperty("transactions").GetArrayLength().Should().Be(2);
        secondStatement.GetProperty("totalAmount").GetDecimal().Should().Be(50m);
    }

    [Fact]
    public async Task PurchasesInDifferentMonths_UseDifferentStatements()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s);

        await CreateCardPurchaseAsync(s, cardId, 30m, month: 2);
        await CreateCardPurchaseAsync(s, cardId, 20m, month: 3);

        var feb = await GetStatementAsync(s, cardId, month: 2);
        var mar = await GetStatementAsync(s, cardId, month: 3);

        feb.GetProperty("statementId").GetGuid().Should().NotBe(mar.GetProperty("statementId").GetGuid());
        feb.GetProperty("totalAmount").GetDecimal().Should().Be(30m);
        mar.GetProperty("totalAmount").GetDecimal().Should().Be(20m);
    }

    [Fact]
    public async Task EditAndDelete_CardPurchaseInAnyStatement_IsAllowed()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s);
        // Fatura de um mês bem no passado: não existe mais fatura "fechada" somente leitura.
        var purchaseId = await CreateCardPurchaseAsync(s, cardId, 30m, month: 1, year: 2024);

        var edit = await s.Client.PatchAsJsonAsync($"/api/transactions/{purchaseId}", new { amount = 45m });
        edit.StatusCode.Should().Be(HttpStatusCode.OK, await edit.Content.ReadAsStringAsync());

        var statement = await GetStatementAsync(s, cardId, month: 1, year: 2024);
        statement.GetProperty("totalAmount").GetDecimal().Should().Be(45m);

        var delete = await s.Client.DeleteAsync($"/api/transactions/{purchaseId}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK, await delete.Content.ReadAsStringAsync());

        var after = await GetStatementAsync(s, cardId, month: 1, year: 2024);
        after.GetProperty("transactions").GetArrayLength().Should().Be(0);
        after.GetProperty("totalAmount").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task EditCardPurchase_MovingToAnotherStatement_UpdatesBothStatements()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s);
        var purchaseId = await CreateCardPurchaseAsync(s, cardId, 30m, month: 2);

        var move = await s.Client.PatchAsJsonAsync($"/api/transactions/{purchaseId}",
            new { statementMonth = 4, statementYear = Year });
        move.StatusCode.Should().Be(HttpStatusCode.OK, await move.Content.ReadAsStringAsync());

        (await GetStatementAsync(s, cardId, month: 2)).GetProperty("transactions").GetArrayLength().Should().Be(0);
        (await GetStatementAsync(s, cardId, month: 4)).GetProperty("transactions").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task CardPurchase_WithoutStatementMonth_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();
        var cardId = await CreateCardAsync(s);

        var response = await s.Client.PostAsJsonAsync("/api/transactions", new
        {
            categoryId = s.CategoryId,
            creditCardId = cardId,
            type = "Expense",
            frequency = "Variable",
            paymentMethod = "CreditCard",
            amount = 10m,
            date = "05/02/2026",
            description = "Sem fatura",
            attributionProfileId = s.ProfileId
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
