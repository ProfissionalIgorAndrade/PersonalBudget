using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

public class SimulatorTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly object EmptyRequest = new
    {
        scenarioName = "Teste",
        impacts = Array.Empty<object>()
    };

    private static object SingleExpense(string startDate) => new
    {
        scenarioName = "Despesa única",
        impacts = new[]
        {
            new { description = "Compra", amount = 50000m, type = "Expense",
                  mode = "Single", startDate, installmentCount = 1 }
        }
    };

    private static object InstallmentExpense(string startDate, int count) => new
    {
        scenarioName = "Parcelada",
        impacts = new[]
        {
            new { description = "Carro", amount = 1500m, type = "Expense",
                  mode = "Installment", startDate, installmentCount = count }
        }
    };

    private static object MonthlyExpense(string startDate) => new
    {
        scenarioName = "Mensal",
        impacts = new[]
        {
            new { description = "Assinatura", amount = 300m, type = "Expense",
                  mode = "Monthly", startDate, installmentCount = 0 }
        }
    };

    private static object OneTimeIncome(string startDate) => new
    {
        scenarioName = "Receita",
        impacts = new[]
        {
            new { description = "Bônus", amount = 10000m, type = "Income",
                  mode = "Single", startDate, installmentCount = 1 }
        }
    };

    // ─── auth ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/simulator/calculate", EmptyRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── empty scenario ────────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_EmptyScenario_ReturnsBaseProjection()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var response = await _client.PostAsJsonAsync("/api/simulator/calculate?months=3", EmptyRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        data.GetProperty("months").GetArrayLength().Should().Be(3);

        // Empty scenario → scenario == base → every delta is 0
        foreach (var m in data.GetProperty("months").EnumerateArray())
            m.GetProperty("balanceDelta").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task Calculate_EmptyScenario_ReturnsBreakdownFields()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var response = await _client.PostAsJsonAsync("/api/simulator/calculate?months=1", EmptyRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        // New breakdown fields must be present
        data.TryGetProperty("baseMonthlyAccountExpense", out _).Should().BeTrue();
        data.TryGetProperty("baseMonthlyCardExpense", out _).Should().BeTrue();
        data.GetProperty("summary").TryGetProperty("horizonTotalIncome", out _).Should().BeTrue();
        data.GetProperty("summary").TryGetProperty("horizonTotalAccountExpense", out _).Should().BeTrue();
        data.GetProperty("summary").TryGetProperty("horizonTotalCardExpense", out _).Should().BeTrue();
    }

    // ─── Single impact ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_SingleExpense_ReducesBalanceOnlyInStartMonth()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var startDate = DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM-01");
        var response = await _client.PostAsJsonAsync("/api/simulator/calculate?months=6",
            SingleExpense(startDate));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body   = await response.Content.ReadFromJsonAsync<JsonElement>();
        var months = body.GetProperty("data").GetProperty("months").EnumerateArray().ToList();

        var startParsed = DateTime.Parse(startDate);
        var targetIdx = months.FindIndex(m =>
            m.GetProperty("year").GetInt32()  == startParsed.Year &&
            m.GetProperty("month").GetInt32() == startParsed.Month);

        targetIdx.Should().BeGreaterThanOrEqualTo(0);
        months[targetIdx].GetProperty("simulatedExpense").GetDecimal().Should().Be(50000m);

        // After the single impact, delta stays constant (no more new impact)
        for (int i = targetIdx + 1; i < months.Count; i++)
            months[i].GetProperty("simulatedExpense").GetDecimal().Should().Be(0m);
    }

    // ─── Installment impact ───────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_Installment_AppliesForExactCount()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var startDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-01");
        var response  = await _client.PostAsJsonAsync("/api/simulator/calculate?months=6",
            InstallmentExpense(startDate, 3));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body   = await response.Content.ReadFromJsonAsync<JsonElement>();
        var months = body.GetProperty("data").GetProperty("months").EnumerateArray().ToList();

        var startParsed   = DateTime.Parse(startDate);
        int firstMonthIdx = months.FindIndex(m =>
            m.GetProperty("year").GetInt32()  == startParsed.Year &&
            m.GetProperty("month").GetInt32() == startParsed.Month);

        firstMonthIdx.Should().BeGreaterThanOrEqualTo(0);

        // Months 0,1,2 from start have the installment
        for (int i = firstMonthIdx; i < Math.Min(firstMonthIdx + 3, months.Count); i++)
            months[i].GetProperty("simulatedExpense").GetDecimal().Should().Be(1500m);

        // Month 3+ has no installment
        if (firstMonthIdx + 3 < months.Count)
            months[firstMonthIdx + 3].GetProperty("simulatedExpense").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task Calculate_Installment_BalanceDeltaAccumulatesOverInstallments()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        // Start from the first projected month so all 3 months are within window
        var startDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-01");
        var response  = await _client.PostAsJsonAsync("/api/simulator/calculate?months=3",
            InstallmentExpense(startDate, 3));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body   = await response.Content.ReadFromJsonAsync<JsonElement>();
        var months = body.GetProperty("data").GetProperty("months").EnumerateArray().ToList();

        months.Should().HaveCount(3);
        var deltas = months.Select(m => m.GetProperty("balanceDelta").GetDecimal()).ToList();
        deltas[0].Should().Be(-1500m);
        deltas[1].Should().Be(-3000m);
        deltas[2].Should().Be(-4500m);
    }

    // ─── Monthly impact ───────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_Monthly_AppliesEachMonthFromStart()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var startDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-01");
        var response  = await _client.PostAsJsonAsync("/api/simulator/calculate?months=3",
            MonthlyExpense(startDate));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body   = await response.Content.ReadFromJsonAsync<JsonElement>();
        var months = body.GetProperty("data").GetProperty("months").EnumerateArray().ToList();

        // All 3 months get the monthly impact
        months.Should().HaveCount(3);
        foreach (var m in months)
            m.GetProperty("simulatedExpense").GetDecimal().Should().Be(300m);
    }

    // ─── Income ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_SingleIncome_IncreasesBalance()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var startDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-01");
        var response  = await _client.PostAsJsonAsync("/api/simulator/calculate?months=3",
            OneTimeIncome(startDate));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body   = await response.Content.ReadFromJsonAsync<JsonElement>();
        var months = body.GetProperty("data").GetProperty("months").EnumerateArray().ToList();

        months[0].GetProperty("balanceDelta").GetDecimal().Should().Be(10000m);
        months[1].GetProperty("balanceDelta").GetDecimal().Should().Be(0m);
    }

    // ─── Negative scenario ────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_NegativeScenario_FlagsGoesNegative()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var startDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-01");
        var massive = new
        {
            scenarioName = "Ruína",
            impacts = new[]
            {
                new { description = "Ruína", amount = 10_000_000m, type = "Expense",
                      mode = "Monthly", startDate, installmentCount = 0 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/simulator/calculate?months=6", massive);
        var body     = await response.Content.ReadFromJsonAsync<JsonElement>();
        var summary  = body.GetProperty("data").GetProperty("summary");

        summary.GetProperty("scenarioGoesNegative").GetBoolean().Should().BeTrue();
        summary.TryGetProperty("firstNegativeMonthIndex", out var idx).Should().BeTrue();
        idx.ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    // ─── Validation ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_InvalidAmount_ReturnsBadRequest()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var bad = new
        {
            scenarioName = "Inválido",
            impacts = new[]
            {
                new { description = "Zero", amount = 0m, type = "Expense",
                      mode = "Single", startDate = "2027-01-01", installmentCount = 1 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/simulator/calculate", bad);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── Horizon ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_With3MonthsHorizon_Returns3Months()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var response = await _client.PostAsJsonAsync("/api/simulator/calculate?months=3", EmptyRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("months").GetArrayLength().Should().Be(3);
    }

    // ─── Data integrity ───────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_DoesNotPersistAnyTransaction()
    {
        var token = await AuthHelper.SignInAsync(_client);
        _client.SetBearer(token);

        var startDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-01");
        await _client.PostAsJsonAsync("/api/simulator/calculate?months=3", SingleExpense(startDate));

        var txResponse = await _client.GetAsync(
            $"/api/transactions?month={DateTime.UtcNow.Month}&year={DateTime.UtcNow.Year}");
        txResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await txResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }
}
