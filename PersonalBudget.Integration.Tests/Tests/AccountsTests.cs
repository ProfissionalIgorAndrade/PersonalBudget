using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// Testa o contrato de contas sem agência/número e com apelido opcional
/// (POST/PUT/GET api/accounts), além das caixinhas e das linhas de transação.
/// Também exercita a migration que remove agency_number e account_number.
/// </summary>
public class AccountsTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private sealed record Scenario(HttpClient Client, Guid ProfileId, string MemberName);

    private async Task<Scenario> CreateScenarioAsync()
    {
        var client = factory.CreateClient();
        client.SetBearer(await AuthHelper.SignInAsync(client));

        var households = await GetDataAsync(await client.GetAsync("/api/households"));
        var householdId = households[0].GetProperty("id").GetGuid();

        var profiles = await GetDataAsync(await client.GetAsync($"/api/households/{householdId}/profiles"));
        var profile = profiles.EnumerateArray()
            .First(p => p.GetProperty("userId").ValueKind != JsonValueKind.Null);

        return new Scenario(
            client,
            profile.GetProperty("id").GetGuid(),
            profile.GetProperty("displayName").GetString()!);
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

    private static async Task<JsonElement> GetAccountAsync(Scenario s, Guid accountId)
    {
        var data = await GetDataAsync(await s.Client.GetAsync("/api/accounts"));
        return data.EnumerateArray().Single(a => a.GetProperty("id").GetGuid() == accountId);
    }

    [Fact]
    public async Task Create_WithoutAgencyAndNumber_ReturnsCreated()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Client.PostAsJsonAsync("/api/accounts",
            new { bank = "Nubank", memberId = s.ProfileId });

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_WithoutNickname_DisplayNameIsBankAndMember()
    {
        var s = await CreateScenarioAsync();
        var id = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", memberId = s.ProfileId });

        var account = await GetAccountAsync(s, id);

        account.GetProperty("displayName").GetString().Should().Be($"Nubank - {s.MemberName}");
        account.GetProperty("name").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Create_WithNickname_ReturnsTrimmedNicknameInDisplayName()
    {
        var s = await CreateScenarioAsync();
        var id = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", name = "  Reserva  ", memberId = s.ProfileId });

        var account = await GetAccountAsync(s, id);

        account.GetProperty("name").GetString().Should().Be("Reserva");
        account.GetProperty("displayName").GetString().Should().Be($"Reserva - {s.MemberName}");
    }

    [Fact]
    public async Task Create_WithBancoDoBrasil_DisplayNameUsesLabelAndKeepsRawBankName()
    {
        var s = await CreateScenarioAsync();
        var id = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "BancoDoBrasil", memberId = s.ProfileId });

        var account = await GetAccountAsync(s, id);

        account.GetProperty("displayName").GetString().Should().Be($"Banco do Brasil - {s.MemberName}");
        account.GetProperty("bank").GetString().Should().Be("BancoDoBrasil");
    }

    [Theory]
    [InlineData("Btg", "BTG Pactual")]
    [InlineData("C6Bank", "C6 Bank")]
    [InlineData("MercadoPago", "Mercado Pago")]
    [InlineData("Outro", "Outro")]
    public async Task Create_WithNewBank_DisplayNameUsesLabel(string bank, string label)
    {
        var s = await CreateScenarioAsync();
        var id = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank, memberId = s.ProfileId });

        var account = await GetAccountAsync(s, id);

        account.GetProperty("displayName").GetString().Should().Be($"{label} - {s.MemberName}");
        account.GetProperty("bank").GetString().Should().Be(bank);
    }

    [Fact]
    public async Task Create_WithUnknownBank_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Client.PostAsJsonAsync("/api/accounts",
            new { bank = "Foo", memberId = s.ProfileId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_WithExistingBanks_KeepsTheSameDisplayName()
    {
        var s = await CreateScenarioAsync();
        var nubankId = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", memberId = s.ProfileId });
        var itauId = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Itau", memberId = s.ProfileId });

        (await GetAccountAsync(s, nubankId)).GetProperty("displayName").GetString()
            .Should().Be($"Nubank - {s.MemberName}");
        (await GetAccountAsync(s, itauId)).GetProperty("displayName").GetString()
            .Should().Be($"Itau - {s.MemberName}");
    }

    [Fact]
    public async Task Create_WithBlankNickname_StoresNull()
    {
        var s = await CreateScenarioAsync();
        var id = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", name = "   ", memberId = s.ProfileId });

        var account = await GetAccountAsync(s, id);

        account.GetProperty("name").ValueKind.Should().Be(JsonValueKind.Null);
        account.GetProperty("displayName").GetString().Should().Be($"Nubank - {s.MemberName}");
    }

    [Fact]
    public async Task GetAll_DoesNotExposeAgencyOrAccountNumber()
    {
        var s = await CreateScenarioAsync();
        var id = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", memberId = s.ProfileId });

        var account = await GetAccountAsync(s, id);

        account.TryGetProperty("agency", out _).Should().BeFalse();
        account.TryGetProperty("accountNumber", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Update_ChangesBankNicknameAndMember()
    {
        var s = await CreateScenarioAsync();
        var id = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", memberId = s.ProfileId });

        var response = await s.Client.PutAsJsonAsync($"/api/accounts/{id}",
            new { bank = "Itau", name = "Viagens", memberId = s.ProfileId });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var account = await GetAccountAsync(s, id);
        account.GetProperty("bank").GetString().Should().Be("Itau");
        account.GetProperty("name").GetString().Should().Be("Viagens");
        account.GetProperty("memberProfileId").GetGuid().Should().Be(s.ProfileId);
        account.GetProperty("displayName").GetString().Should().Be($"Viagens - {s.MemberName}");

        var clear = await s.Client.PutAsJsonAsync($"/api/accounts/{id}",
            new { bank = "Itau", name = "", memberId = s.ProfileId });
        clear.StatusCode.Should().Be(HttpStatusCode.OK);

        var cleared = await GetAccountAsync(s, id);
        cleared.GetProperty("name").ValueKind.Should().Be(JsonValueKind.Null);
        cleared.GetProperty("displayName").GetString().Should().Be($"Itau - {s.MemberName}");
    }

    [Fact]
    public async Task Summary_UsesSameDisplayNameRuleAsList()
    {
        var s = await CreateScenarioAsync();
        var accountId = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", name = "Principal", memberId = s.ProfileId });
        var boxId = await PostAndGetIdAsync(s.Client, "/api/accounts/savings-boxes",
            new { parentAccountId = accountId, name = "Viagem" });

        var summary = await GetDataAsync(await s.Client.GetAsync("/api/accounts/summary"));
        var items = summary.GetProperty("accounts").EnumerateArray().ToList();

        items.Single(i => i.GetProperty("id").GetGuid() == accountId)
            .GetProperty("name").GetString().Should().Be($"Principal - {s.MemberName}");
        items.Single(i => i.GetProperty("id").GetGuid() == boxId)
            .GetProperty("name").GetString().Should().Be("Viagem");
    }

    [Fact]
    public async Task CreateSavingsBox_StillWorks_AndInheritsBankAndMember()
    {
        var s = await CreateScenarioAsync();
        var accountId = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", name = "Principal", memberId = s.ProfileId });

        var boxId = await PostAndGetIdAsync(s.Client, "/api/accounts/savings-boxes",
            new { parentAccountId = accountId, name = "Viagem" });

        var box = await GetAccountAsync(s, boxId);
        box.GetProperty("kind").GetString().Should().Be("Savings");
        box.GetProperty("name").GetString().Should().Be("Viagem");
        box.GetProperty("displayName").GetString().Should().Be("Viagem");
        box.GetProperty("bank").GetString().Should().Be("Nubank");
        box.GetProperty("parentAccountId").GetGuid().Should().Be(accountId);
        box.GetProperty("memberProfileId").GetGuid().Should().Be(s.ProfileId);
    }

    [Fact]
    public async Task SavingsBox_RenameAndGoal_RejectCheckingAccount()
    {
        var s = await CreateScenarioAsync();
        var accountId = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", memberId = s.ProfileId });

        var rename = await s.Client.PatchAsJsonAsync(
            $"/api/accounts/savings-boxes/{accountId}", new { name = "X" });
        var goal = await s.Client.PatchAsJsonAsync(
            $"/api/accounts/savings-boxes/{accountId}/goal", new { goal = 100m });

        rename.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        goal.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransactionRows_DoNotExposeAccountName()
    {
        var s = await CreateScenarioAsync();
        var accountId = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", memberId = s.ProfileId });
        var categoryId = await PostAndGetIdAsync(s.Client, "/api/categories",
            new { name = "Mercado", type = "Expense" });
        var txId = await PostAndGetIdAsync(s.Client, "/api/transactions", new
        {
            accountId,
            categoryId,
            type = "Expense",
            frequency = "Variable",
            paymentMethod = "Account",
            amount = 10m,
            date = "05/10/2026",
            description = "Compra conta",
            attributionProfileId = s.ProfileId
        });

        var byAccount = await GetDataAsync(
            await s.Client.GetAsync($"/api/accounts/{accountId}/transactions?month=10&year=2026"));
        var all = await GetDataAsync(await s.Client.GetAsync("/api/transactions"));
        var byMonth = await GetDataAsync(
            await s.Client.GetAsync("/api/transactions/month/10/year/2026"));

        foreach (var rows in new[] { byAccount, all, byMonth })
        {
            var row = rows.EnumerateArray().Single(t => t.GetProperty("id").GetGuid() == txId);
            row.TryGetProperty("accountName", out _).Should().BeFalse();
            row.GetProperty("accountId").GetGuid().Should().Be(accountId);
        }
    }

    [Fact]
    public async Task CardPurchase_DoesNotChangeBalanceNorAppearInAccountTransactions()
    {
        var s = await CreateScenarioAsync();
        var accountId = await PostAndGetIdAsync(s.Client, "/api/accounts",
            new { bank = "Nubank", memberId = s.ProfileId });
        var categoryId = await PostAndGetIdAsync(s.Client, "/api/categories",
            new { name = "Mercado", type = "Expense" });
        var cardId = await PostAndGetIdAsync(s.Client, "/api/credit-cards",
            new { name = "Cartao", limit = 5000m, dueDay = 10, memberId = s.ProfileId });

        await PostAndGetIdAsync(s.Client, "/api/transactions", new
        {
            accountId,
            categoryId,
            type = "Income",
            frequency = "Variable",
            paymentMethod = "Account",
            amount = 200m,
            date = "05/10/2026",
            description = "Entrada",
            attributionProfileId = s.ProfileId
        });
        var purchaseId = await PostAndGetIdAsync(s.Client, "/api/transactions", new
        {
            categoryId,
            creditCardId = cardId,
            type = "Expense",
            frequency = "Variable",
            paymentMethod = "CreditCard",
            amount = 80m,
            date = "05/10/2026",
            description = "Compra cartao",
            statementMonth = 10,
            statementYear = 2026,
            attributionProfileId = s.ProfileId
        });

        var account = await GetAccountAsync(s, accountId);
        account.GetProperty("balance").GetDecimal().Should().Be(200m);

        var byAccount = await GetDataAsync(
            await s.Client.GetAsync($"/api/accounts/{accountId}/transactions?month=10&year=2026"));
        byAccount.EnumerateArray()
            .Should().NotContain(t => t.GetProperty("id").GetGuid() == purchaseId);
    }
}
