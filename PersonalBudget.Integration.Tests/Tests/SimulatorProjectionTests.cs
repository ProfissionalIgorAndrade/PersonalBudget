using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// POST /api/simulator/projection. Todo pedido manda "today" explícito (2026-10-15) para não
/// depender do relógio; o horizonte padrão dos testes é de 3 meses (out, nov, dez de 2026).
/// Os históricos de uma casa nova são vazios, então as médias são nulas e o baseline mostra
/// só o que foi lançado.
/// </summary>
public class SimulatorProjectionTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private const string Today = "2026-10-15";

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
            new { name = "Geral", type = "Expense" });

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

    private static Task<Guid> CreateAccountAsync(Scenario s) =>
        PostAndGetIdAsync(s.Client, "/api/accounts", new { bank = "Nubank", memberId = s.ProfileId });

    private static Task<Guid> CreateCardAsync(Scenario s) =>
        PostAndGetIdAsync(s.Client, "/api/credit-cards",
            new { name = "Cartao", limit = 5000m, dueDay = 10, memberId = s.ProfileId });

    private static Task<Guid> AddAccountRowAsync(
        Scenario s, Guid accountId, string type, string frequency, decimal amount, string date,
        int? repeatCount = null, int? dueDay = null) =>
        PostAndGetIdAsync(s.Client, "/api/transactions", new
        {
            accountId,
            categoryId = s.CategoryId,
            type,
            frequency,
            paymentMethod = "Account",
            amount,
            date,
            description = "Lançamento",
            repeatCount,
            dueDay,
            attributionProfileId = s.ProfileId
        });

    private static Task<Guid> AddCardPurchaseAsync(
        Scenario s, Guid cardId, decimal amount, string date, int statementMonth, int statementYear) =>
        PostAndGetIdAsync(s.Client, "/api/transactions", new
        {
            categoryId = s.CategoryId,
            creditCardId = cardId,
            type = "Expense",
            frequency = "Variable",
            paymentMethod = "CreditCard",
            amount,
            date,
            description = "Compra cartao",
            statementMonth,
            statementYear,
            attributionProfileId = s.ProfileId
        });

    private static Task<Guid> AddCardInstallmentsAsync(
        Scenario s, Guid cardId, decimal total, int count, int statementMonth, int statementYear) =>
        PostAndGetIdAsync(s.Client, "/api/transactions", new
        {
            categoryId = s.CategoryId,
            creditCardId = cardId,
            type = "Expense",
            frequency = "Installments",
            paymentMethod = "CreditCard",
            amount = total / count,
            totalAmount = total,
            installmentCount = count,
            date = "05/10/2026",
            description = "Parcelado",
            statementMonth,
            statementYear,
            attributionProfileId = s.ProfileId
        });

    private static object Body(
        int months = 3, string? today = Today, params object[] impacts) =>
        new { today, months, impacts };

    private static object Impact(
        string id, string type, string mode, string startMonth, decimal amount,
        string? amountKind = null, int? installments = null, int? months = null,
        string description = "Teste") =>
        new { id, description, type, mode, startMonth, amount, amountKind, installments, months };

    private static async Task<JsonElement> ProjectAsync(Scenario s, object body) =>
        await GetDataAsync(await s.Client.PostAsJsonAsync("/api/simulator/projection", body));

    private static decimal[] Column(JsonElement data, string property) =>
        data.GetProperty("baseline").EnumerateArray()
            .Select(m => m.GetProperty(property).GetDecimal()).ToArray();

    private static async Task<int> CountTransactionsAsync(Scenario s) =>
        (await GetDataAsync(await s.Client.GetAsync("/api/transactions"))).GetArrayLength();

    // ─── auth ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Projection_WithoutAuth_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/simulator/projection", Body());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── opening balance ────────────────────────────────────────────────────

    [Fact]
    public async Task OpeningBalance_ExcludesSavingsBoxAndFutureDatedRows()
    {
        var s = await CreateScenarioAsync();
        var accountId = await CreateAccountAsync(s);
        await AddAccountRowAsync(s, accountId, "Income", "Variable", 1000m, "05/10/2026");
        await AddAccountRowAsync(s, accountId, "Expense", "Variable", 200m, "10/10/2026");
        await AddAccountRowAsync(s, accountId, "Income", "Variable", 300m, "20/10/2026");   // depois de hoje
        var boxId = await PostAndGetIdAsync(s.Client, "/api/accounts/savings-boxes",
            new { parentAccountId = accountId, name = "Viagem" });
        (await s.Client.PostAsJsonAsync($"/api/accounts/savings-boxes/{boxId}/deposit",
            new { amount = 500m, reason = "teste" })).StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await ProjectAsync(s, Body());

        var opening = data.GetProperty("openingBalance");
        opening.GetProperty("amount").GetDecimal().Should().Be(800m);
        opening.GetProperty("asOf").GetString().Should().Be("2026-10-15");
        opening.GetProperty("excludesSavings").GetBoolean().Should().BeTrue();
        var accounts = opening.GetProperty("accounts").EnumerateArray().ToList();
        accounts.Should().ContainSingle();
        accounts[0].GetProperty("id").GetGuid().Should().Be(accountId);
        accounts[0].GetProperty("balance").GetDecimal().Should().Be(800m);
        data.GetProperty("referenceMonth").GetString().Should().Be("2026-10");
    }

    [Fact]
    public async Task OpeningBalance_IgnoresDeletedAccounts()
    {
        var s = await CreateScenarioAsync();
        var kept = await CreateAccountAsync(s);
        var removed = await CreateAccountAsync(s);
        await AddAccountRowAsync(s, kept, "Income", "Variable", 100m, "01/10/2026");
        await AddAccountRowAsync(s, removed, "Income", "Variable", 400m, "01/10/2026");
        (await s.Client.DeleteAsync($"/api/accounts/{removed}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await ProjectAsync(s, Body());

        data.GetProperty("openingBalance").GetProperty("amount").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task OpeningBalance_WithoutAccounts_IsZero()
    {
        var s = await CreateScenarioAsync();

        var data = await ProjectAsync(s, Body());

        data.GetProperty("openingBalance").GetProperty("amount").GetDecimal().Should().Be(0m);
        data.GetProperty("openingBalance").GetProperty("accounts").GetArrayLength().Should().Be(0);
        data.GetProperty("baseline").GetArrayLength().Should().Be(3);
        data.GetProperty("assumptions").GetProperty("averageIncome").ValueKind.Should().Be(JsonValueKind.Null);
    }

    // ─── baseline ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Baseline_UsesRealFutureFixedRowsAndCardInstallmentsByStatementMonth()
    {
        var s = await CreateScenarioAsync();
        var accountId = await CreateAccountAsync(s);
        var cardId = await CreateCardAsync(s);
        await AddAccountRowAsync(s, accountId, "Income", "Variable", 2000m, "01/10/2026");
        // aluguel fixo: nov, dez e jan
        await AddAccountRowAsync(s, accountId, "Expense", "Fixed", 800m, "05/11/2026", repeatCount: 3, dueDay: 5);
        // 3x de 100 na fatura de nov, dez e jan
        await AddCardInstallmentsAsync(s, cardId, 300m, 3, statementMonth: 11, statementYear: 2026);

        var data = await ProjectAsync(s, Body());

        Column(data, "committed").Should().Equal(0m, 900m, 900m);
        Column(data, "income").Should().Equal(0m, 0m, 0m);
        Column(data, "variable").Should().Equal(0m, 0m, 0m);
        Column(data, "result").Should().Equal(0m, -900m, -900m);
        Column(data, "balance").Should().Equal(2000m, 1100m, 200m);
        data.GetProperty("baseline")[1].GetProperty("label").GetString().Should().Be("nov/26");
    }

    [Fact]
    public async Task Baseline_ExcludesTransfersFromIncomeAndExpense()
    {
        var s = await CreateScenarioAsync();
        var from = await CreateAccountAsync(s);
        var to = await CreateAccountAsync(s);
        await AddAccountRowAsync(s, from, "Income", "Variable", 1000m, "01/10/2026");

        foreach (var date in new[] { "10/10/2026", "20/10/2026" })   // uma até hoje, outra depois
        {
            (await s.Client.PostAsJsonAsync("/api/transactions", new
            {
                fromAccountId = from,
                toAccountId = to,
                type = "Expense",
                frequency = "Variable",
                paymentMethod = "Transfer",
                amount = 300m,
                date,
                description = "Transferência",
                attributionProfileId = s.ProfileId
            })).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var data = await ProjectAsync(s, Body());

        // A transferência até hoje move dinheiro entre contas correntes: o total não muda.
        data.GetProperty("openingBalance").GetProperty("amount").GetDecimal().Should().Be(1000m);
        // A transferência futura não vira receita nem despesa.
        Column(data, "income").Should().Equal(0m, 0m, 0m);
        Column(data, "committed").Should().Equal(0m, 0m, 0m);
        Column(data, "variable").Should().Equal(0m, 0m, 0m);
    }

    [Fact]
    public async Task Baseline_ExcludesSavingsMovementsFromIncomeAndExpense()
    {
        var s = await CreateScenarioAsync();
        var accountId = await CreateAccountAsync(s);
        var boxId = await PostAndGetIdAsync(s.Client, "/api/accounts/savings-boxes",
            new { parentAccountId = accountId, name = "Reserva" });
        // O movimento de caixinha tem a data real de hoje: com "today" ontem ele cai depois de hoje
        // e apareceria no fluxo se não fosse excluído.
        (await s.Client.PostAsJsonAsync($"/api/accounts/savings-boxes/{boxId}/deposit",
            new { amount = 500m })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await s.Client.PostAsJsonAsync($"/api/accounts/savings-boxes/{boxId}/withdraw",
            new { amount = 100m })).StatusCode.Should().Be(HttpStatusCode.OK);
        var yesterday = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");

        var data = await ProjectAsync(s, Body(months: 3, today: yesterday));

        Column(data, "income").Should().Equal(0m, 0m, 0m);
        Column(data, "committed").Should().Equal(0m, 0m, 0m);
        Column(data, "variable").Should().Equal(0m, 0m, 0m);
        data.GetProperty("openingBalance").GetProperty("amount").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task CardPurchase_LeavesTheOpeningBalanceAndAppearsInItsStatementMonth()
    {
        var s = await CreateScenarioAsync();
        var accountId = await CreateAccountAsync(s);
        var cardId = await CreateCardAsync(s);
        await AddAccountRowAsync(s, accountId, "Income", "Variable", 500m, "01/10/2026");

        var before = await ProjectAsync(s, Body());
        await AddCardPurchaseAsync(s, cardId, 80m, "05/10/2026", statementMonth: 11, statementYear: 2026);
        var after = await ProjectAsync(s, Body());

        before.GetProperty("openingBalance").GetProperty("amount").GetDecimal().Should().Be(500m);
        after.GetProperty("openingBalance").GetProperty("amount").GetDecimal().Should().Be(500m);
        // A compra é de out/26, mas a fatura é de nov/26.
        Column(after, "variable").Should().Equal(0m, 80m, 0m);
        Column(after, "balance").Should().Equal(500m, 420m, 420m);
    }

    [Fact]
    public async Task ReferenceMonth_CountsTheWholeCardStatementButNotAccountRowsAlreadyInTheBalance()
    {
        var s = await CreateScenarioAsync();
        var accountId = await CreateAccountAsync(s);
        var cardId = await CreateCardAsync(s);
        await AddAccountRowAsync(s, accountId, "Income", "Variable", 1000m, "05/10/2026");          // até hoje
        await AddAccountRowAsync(s, accountId, "Expense", "Variable", 100m, "10/10/2026");          // até hoje
        await AddAccountRowAsync(s, accountId, "Expense", "Variable", 50m, "20/10/2026");           // depois de hoje
        await AddCardPurchaseAsync(s, cardId, 70m, "01/10/2026", statementMonth: 10, statementYear: 2026);

        var data = await ProjectAsync(s, Body(months: 1));

        data.GetProperty("openingBalance").GetProperty("amount").GetDecimal().Should().Be(900m);
        Column(data, "income").Should().Equal(0m);
        Column(data, "variable").Should().Equal(120m);     // 50 de conta futura + 70 de cartão
        Column(data, "balance").Should().Equal(780m);
    }

    // ─── impacts ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeveralImpacts_AddUpInTheScenario()
    {
        var s = await CreateScenarioAsync();
        var accountId = await CreateAccountAsync(s);
        await AddAccountRowAsync(s, accountId, "Income", "Variable", 1000m, "01/10/2026");

        var data = await ProjectAsync(s, Body(3, Today,
            Impact("a", "Expense", "Single", "2026-11", 200m),
            Impact("b", "Expense", "Installment", "2026-11", 300m, amountKind: "Total", installments: 3),
            Impact("c", "Income", "Monthly", "2026-12", 50m)));

        var scenario = data.GetProperty("scenario").EnumerateArray().ToList();
        scenario[1].GetProperty("simulatedExpense").GetDecimal().Should().Be(300m);   // 200 + 100
        scenario[1].GetProperty("simulatedIncome").GetDecimal().Should().Be(0m);
        scenario[2].GetProperty("simulatedExpense").GetDecimal().Should().Be(100m);
        scenario[2].GetProperty("simulatedIncome").GetDecimal().Should().Be(50m);
        scenario.Select(m => m.GetProperty("balance").GetDecimal()).Should().Equal(1000m, 700m, 650m);
        scenario.Select(m => m.GetProperty("delta").GetDecimal()).Should().Equal(0m, -300m, -350m);

        var impacts = data.GetProperty("impacts").EnumerateArray().ToList();
        impacts.Select(i => i.GetProperty("id").GetString()).Should().Equal("a", "b", "c");
        impacts[1].GetProperty("monthly").EnumerateArray().Select(v => v.GetDecimal())
            .Should().Equal(0m, -100m, -100m);
        impacts[1].GetProperty("totalInHorizon").GetDecimal().Should().Be(-200m);
        impacts[1].GetProperty("totalFull").GetDecimal().Should().Be(-300m);
        impacts[1].GetProperty("installmentAmount").GetDecimal().Should().Be(100m);
        impacts[1].GetProperty("lastInstallmentAmount").GetDecimal().Should().Be(100m);
        impacts[1].GetProperty("installmentsInHorizon").GetInt32().Should().Be(2);
        impacts[1].GetProperty("installmentsTotal").GetInt32().Should().Be(3);

        var summary = data.GetProperty("summary");
        summary.GetProperty("totalImpactInHorizon").GetDecimal().Should().Be(-350m);   // a -200, b -200 (nov e dez), c +50 (dez)
        summary.GetProperty("endBalanceBaseline").GetDecimal().Should().Be(1000m);
        summary.GetProperty("endBalanceScenario").GetDecimal().Should().Be(650m);
        summary.GetProperty("scenarioMinBalance").GetProperty("amount").GetDecimal().Should().Be(650m);
        summary.GetProperty("scenarioMinBalance").GetProperty("monthIndex").GetInt32().Should().Be(2);
        summary.GetProperty("baselineMinBalance").GetProperty("monthIndex").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task ImpactsOutsideTheWindow_ComeBackWithWarningsAndBothTotals()
    {
        var s = await CreateScenarioAsync();

        var data = await ProjectAsync(s, Body(3, Today,
            Impact("long", "Expense", "Installment", "2026-12", 1200m, amountKind: "Total", installments: 12),
            Impact("old", "Expense", "Installment", "2026-01", 100m, installments: 3)));

        var impacts = data.GetProperty("impacts").EnumerateArray().ToList();
        impacts[0].GetProperty("totalInHorizon").GetDecimal().Should().Be(-100m);
        impacts[0].GetProperty("totalFull").GetDecimal().Should().Be(-1200m);
        impacts[1].GetProperty("totalInHorizon").GetDecimal().Should().Be(0m);
        impacts[1].GetProperty("totalFull").GetDecimal().Should().Be(-300m);

        var warnings = data.GetProperty("warnings").EnumerateArray()
            .Select(w => $"{w.GetProperty("impactId").GetString()}:{w.GetProperty("code").GetString()}").ToList();
        warnings.Should().Equal("long:AfterWindow", "old:BeforeWindow");
        data.GetProperty("warnings")[0].GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetProperty("summary").GetProperty("totalImpactFull").GetDecimal().Should().Be(-1500m);
    }

    [Fact]
    public async Task Projection_ReturnsAssumptionsWithNotes()
    {
        var s = await CreateScenarioAsync();

        var data = await ProjectAsync(s, Body());

        var assumptions = data.GetProperty("assumptions");
        assumptions.GetProperty("lookbackMonths").GetInt32().Should().Be(3);
        assumptions.GetProperty("notes").GetArrayLength().Should().BeGreaterThan(0);
    }

    // ─── isolation and persistence ──────────────────────────────────────────

    [Fact]
    public async Task AnotherHouseholdsData_IsNotUsed()
    {
        var other = await CreateScenarioAsync();
        var otherAccount = await CreateAccountAsync(other);
        var otherCard = await CreateCardAsync(other);
        await AddAccountRowAsync(other, otherAccount, "Income", "Variable", 9000m, "01/10/2026");
        await AddAccountRowAsync(other, otherAccount, "Expense", "Fixed", 700m, "05/11/2026", repeatCount: 2, dueDay: 5);
        await AddCardPurchaseAsync(other, otherCard, 120m, "05/10/2026", statementMonth: 11, statementYear: 2026);

        var mine = await CreateScenarioAsync();
        var data = await ProjectAsync(mine, Body());

        data.GetProperty("openingBalance").GetProperty("amount").GetDecimal().Should().Be(0m);
        data.GetProperty("openingBalance").GetProperty("accounts").GetArrayLength().Should().Be(0);
        Column(data, "income").Should().Equal(0m, 0m, 0m);
        Column(data, "committed").Should().Equal(0m, 0m, 0m);
        Column(data, "variable").Should().Equal(0m, 0m, 0m);
    }

    [Fact]
    public async Task Projection_DoesNotCreateOrChangeAnyTransaction()
    {
        var s = await CreateScenarioAsync();
        var accountId = await CreateAccountAsync(s);
        await AddAccountRowAsync(s, accountId, "Income", "Variable", 1000m, "01/10/2026");
        var countBefore = await CountTransactionsAsync(s);
        var balanceBefore = (await GetDataAsync(await s.Client.GetAsync("/api/accounts")))[0]
            .GetProperty("balance").GetDecimal();

        await ProjectAsync(s, Body(6, Today,
            Impact("a", "Expense", "Single", "2026-11", 200m),
            Impact("b", "Expense", "Monthly", "2026-10", 50m),
            Impact("c", "Income", "Installment", "2026-12", 600m, amountKind: "Total", installments: 4)));

        (await CountTransactionsAsync(s)).Should().Be(countBefore);
        (await GetDataAsync(await s.Client.GetAsync("/api/accounts")))[0]
            .GetProperty("balance").GetDecimal().Should().Be(balanceBefore);
    }

    // ─── validation (400) ───────────────────────────────────────────────────

    private async Task AssertBadRequestAsync(object body, string? messagePart = null)
    {
        var s = await CreateScenarioAsync();
        var response = await s.Client.PostAsJsonAsync("/api/simulator/projection", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
        if (messagePart is not null)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            json.GetProperty("success").GetBoolean().Should().BeFalse();
            json.GetProperty("message").GetString().Should().Contain(messagePart);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public Task Amount_NotPositive_Returns400NamingTheImpact(int amount) =>
        AssertBadRequestAsync(
            Body(3, Today,
                Impact("a", "Expense", "Single", "2026-11", 10m),
                Impact("b", "Expense", "Single", "2026-11", amount, description: "Viagem")),
            "Impacto #2 (\"Viagem\"): o valor deve ser maior que zero");

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public Task Installments_OutsideOneTo120_Returns400(int installments) =>
        AssertBadRequestAsync(
            Body(3, Today, Impact("a", "Expense", "Installment", "2026-11", 10m, installments: installments)),
            "número de parcelas");

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public Task Horizon_OutsideOneTo24_Returns400(int months) =>
        AssertBadRequestAsync(Body(months), "entre 1 e 24");

    [Fact]
    public Task MoreThanFiftyImpacts_Returns400() =>
        AssertBadRequestAsync(
            Body(3, Today, Enumerable.Range(0, 51)
                .Select(i => Impact($"i{i}", "Expense", "Single", "2026-11", 10m)).ToArray()),
            "No máximo 50");

    [Theory]
    [InlineData("2026-13")]
    [InlineData("11/2026")]
    [InlineData("2026-11-01")]
    [InlineData("")]
    public Task InvalidStartMonth_Returns400(string startMonth) =>
        AssertBadRequestAsync(
            Body(3, Today, Impact("a", "Expense", "Single", startMonth, 10m)),
            "mês de início inválido");

    [Fact]
    public Task InvalidToday_Returns400() =>
        AssertBadRequestAsync(Body(3, "15/10/2026"), "yyyy-MM-dd");

    [Theory]
    [InlineData("Bogus", "Single", null)]
    [InlineData("Expense", "Bogus", null)]
    [InlineData("Expense", "Installment", "Bogus")]
    public Task InvalidEnumValue_Returns400(string type, string mode, string? amountKind) =>
        AssertBadRequestAsync(
            Body(3, Today, Impact("a", type, mode, "2026-11", 10m, amountKind: amountKind, installments: 3)));

    [Fact]
    public async Task MissingBody_Returns400()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Client.PostAsync("/api/simulator/projection",
            new StringContent("", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ErrorResponses_UseTheApiEnvelope()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Client.PostAsJsonAsync("/api/simulator/projection", Body(0));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.TryGetProperty("data", out var data).Should().BeTrue();
        data.ValueKind.Should().Be(JsonValueKind.Null);
    }
}
