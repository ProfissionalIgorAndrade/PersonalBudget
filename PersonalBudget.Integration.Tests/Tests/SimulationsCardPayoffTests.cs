using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// api/simulations no modo CardPayoff: quitar de uma vez as parcelas futuras de um ou mais cartões.
/// O servidor guarda só o mês e os cartões (nulo = todos); o valor é calculado no cliente.
/// </summary>
public class SimulationsCardPayoffTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private sealed record TestUser(HttpClient Client, Guid HouseholdId, Guid ProfileId);

    private async Task<TestUser> CreateUserAsync()
    {
        var client = factory.CreateClient();
        client.SetBearer(await AuthHelper.SignInAsync(client, $"payoff_{Guid.NewGuid():N}@test.com"));

        var households = await GetDataAsync(await client.GetAsync("/api/households"));
        var householdId = households[0].GetProperty("id").GetGuid();
        var profiles = await GetDataAsync(await client.GetAsync($"/api/households/{householdId}/profiles"));
        var profileId = profiles.EnumerateArray()
            .First(p => p.GetProperty("userId").ValueKind != JsonValueKind.Null)
            .GetProperty("id").GetGuid();

        return new TestUser(client, householdId, profileId);
    }

    private static async Task<Guid> CreateCardAsync(TestUser user, string name)
    {
        var response = await user.Client.PostAsJsonAsync("/api/credit-cards",
            new { name, limit = 5000m, dueDay = 10, memberId = user.ProfileId });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> GetDataAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"unexpected response: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data");
    }

    private static object Payoff(Guid[]? creditCardIds, string startMonth = "2026-12", string description = "Zerar cartões") =>
        new { description, type = "Expense", mode = "CardPayoff", startMonth, amount = 0m, creditCardIds };

    private static async Task<Guid> CreateAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/simulations", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> GetOneAsync(HttpClient client, Guid id)
    {
        var data = await GetDataAsync(await client.GetAsync("/api/simulations"));
        return data.EnumerateArray().Single(s => s.GetProperty("id").GetGuid() == id);
    }

    private static Guid[] CardIds(JsonElement sim) =>
        sim.GetProperty("creditCardIds").EnumerateArray().Select(e => e.GetGuid()).ToArray();

    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("message").GetString()!;
    }

    [Fact]
    public async Task Create_AllCards_WithoutAmount_StoresNullCards()
    {
        var user = await CreateUserAsync();

        var id = await CreateAsync(user.Client, Payoff(null));

        var sim = await GetOneAsync(user.Client, id);
        sim.GetProperty("mode").GetString().Should().Be("CardPayoff");
        sim.GetProperty("type").GetString().Should().Be("Expense");
        sim.GetProperty("startMonth").GetString().Should().Be("2026-12");
        sim.GetProperty("amount").GetDecimal().Should().Be(0m);
        sim.GetProperty("creditCardIds").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Create_SomeCards_RoundTrips()
    {
        var user = await CreateUserAsync();
        var nubank = await CreateCardAsync(user, "Nubank");
        var itau = await CreateCardAsync(user, "Itau");

        var id = await CreateAsync(user.Client, Payoff([nubank, itau]));

        CardIds(await GetOneAsync(user.Client, id)).Should().Equal(nubank, itau);
    }

    [Fact]
    public async Task Update_ChangesCardsAndMonth_ThenSwitchesToSingle()
    {
        var user = await CreateUserAsync();
        var nubank = await CreateCardAsync(user, "Nubank");
        var itau = await CreateCardAsync(user, "Itau");
        var id = await CreateAsync(user.Client, Payoff([nubank]));

        var put = await user.Client.PutAsJsonAsync($"/api/simulations/{id}", Payoff([itau], "2027-01"));
        put.StatusCode.Should().Be(HttpStatusCode.OK, await put.Content.ReadAsStringAsync());
        var sim = await GetOneAsync(user.Client, id);
        CardIds(sim).Should().Equal(itau);
        sim.GetProperty("startMonth").GetString().Should().Be("2027-01");

        var single = await user.Client.PutAsJsonAsync($"/api/simulations/{id}",
            new { description = "Carro", type = "Expense", mode = "Single", startMonth = "2027-01", amount = 100m, creditCardIds = new[] { itau } });
        single.StatusCode.Should().Be(HttpStatusCode.OK, await single.Content.ReadAsStringAsync());
        (await GetOneAsync(user.Client, id)).GetProperty("creditCardIds").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Delete_RemovesIt()
    {
        var user = await CreateUserAsync();
        var id = await CreateAsync(user.Client, Payoff(null));

        (await user.Client.DeleteAsync($"/api/simulations/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await GetDataAsync(await user.Client.GetAsync("/api/simulations"));
        data.EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithCardFromAnotherHousehold_ReturnsBadRequest()
    {
        var user = await CreateUserAsync();
        var stranger = await CreateUserAsync();
        var foreignCard = await CreateCardAsync(stranger, "Alheio");

        var response = await user.Client.PostAsJsonAsync("/api/simulations", Payoff([foreignCard]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Be("Cartão não encontrado neste lar.");
    }

    [Fact]
    public async Task Update_WithCardFromAnotherHousehold_ReturnsBadRequest_AndKeepsData()
    {
        var user = await CreateUserAsync();
        var mine = await CreateCardAsync(user, "Meu");
        var stranger = await CreateUserAsync();
        var foreignCard = await CreateCardAsync(stranger, "Alheio");
        var id = await CreateAsync(user.Client, Payoff([mine]));

        var put = await user.Client.PutAsJsonAsync($"/api/simulations/{id}", Payoff([foreignCard]));

        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CardIds(await GetOneAsync(user.Client, id)).Should().Equal(mine);
    }

    [Fact]
    public async Task Create_WithEmptyCardList_ReturnsBadRequest()
    {
        var user = await CreateUserAsync();

        var response = await user.Client.PostAsJsonAsync("/api/simulations", Payoff([]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Contain("escolha ao menos um cartão");
    }

    [Fact]
    public async Task Create_AsIncome_ReturnsBadRequest()
    {
        var user = await CreateUserAsync();

        var response = await user.Client.PostAsJsonAsync("/api/simulations",
            new { description = "X", type = "Income", mode = "CardPayoff", startMonth = "2026-12", amount = 0m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Contain("sempre uma despesa");
    }

    [Fact]
    public async Task Import_AcceptsPayoff_AndRejectsForeignCard()
    {
        var user = await CreateUserAsync();
        var mine = await CreateCardAsync(user, "Meu");
        var stranger = await CreateUserAsync();
        var foreignCard = await CreateCardAsync(stranger, "Alheio");

        var ok = await user.Client.PostAsJsonAsync("/api/simulations/import",
            new { simulations = new[] { Payoff([mine]), Payoff(null, description: "Todos") } });
        ok.StatusCode.Should().Be(HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());

        var bad = await user.Client.PostAsJsonAsync("/api/simulations/import",
            new { simulations = new[] { Payoff([foreignCard]) } });
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var data = await GetDataAsync(await user.Client.GetAsync("/api/simulations"));
        data.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task OldModes_StillReturnNullCards()
    {
        var user = await CreateUserAsync();
        var id = await CreateAsync(user.Client,
            new { description = "Carro", type = "Expense", mode = "Installment", startMonth = "2026-11", amount = 100m, installments = 3 });

        var sim = await GetOneAsync(user.Client, id);
        sim.GetProperty("creditCardIds").ValueKind.Should().Be(JsonValueKind.Null);
        sim.GetProperty("installments").GetInt32().Should().Be(3);
    }
}
