using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// Exclusão de caixinha com motivo (DELETE api/accounts/savings-boxes/{id}) e histórico de
/// eventos (GET api/accounts/savings-box-events). Cada cenário usa um usuário novo, e portanto um lar novo.
/// </summary>
public class SavingsBoxDeleteTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private sealed record Scenario(HttpClient Client, Guid ProfileId, Guid CheckingId);

    private async Task<Scenario> CreateScenarioAsync()
    {
        var client = factory.CreateClient();
        client.SetBearer(await AuthHelper.SignInAsync(client));

        var households = await GetDataAsync(await client.GetAsync("/api/households"));
        var householdId = households[0].GetProperty("id").GetGuid();

        var profiles = await GetDataAsync(await client.GetAsync($"/api/households/{householdId}/profiles"));
        var profile = profiles.EnumerateArray()
            .First(p => p.GetProperty("userId").ValueKind != JsonValueKind.Null);
        var profileId = profile.GetProperty("id").GetGuid();

        var checkingId = await PostAndGetIdAsync(client, "/api/accounts",
            new { bank = "Nubank", memberId = profileId });

        return new Scenario(client, profileId, checkingId);
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
        return body.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static Task<Guid> CreateBoxAsync(Scenario s, string name) =>
        PostAndGetIdAsync(s.Client, "/api/accounts/savings-boxes",
            new { parentAccountId = s.CheckingId, name });

    private static async Task DepositAsync(Scenario s, Guid boxId, decimal amount)
    {
        var response = await s.Client.PostAsJsonAsync(
            $"/api/accounts/savings-boxes/{boxId}/deposit", new { amount });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private static Task<HttpResponseMessage> DeleteBoxAsync(HttpClient client, Guid boxId, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/accounts/savings-boxes/{boxId}")
        {
            Content = JsonContent.Create(body)
        };
        return client.SendAsync(request);
    }

    private static async Task<List<JsonElement>> GetAccountsAsync(HttpClient client) =>
        (await GetDataAsync(await client.GetAsync("/api/accounts"))).EnumerateArray().ToList();

    private static async Task<List<JsonElement>> GetEventsAsync(HttpClient client) =>
        (await GetDataAsync(await client.GetAsync("/api/accounts/savings-box-events"))).EnumerateArray().ToList();

    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static async Task ShouldBeBadRequestAsync(HttpResponseMessage response, string messagePart)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().ContainEquivalentOf(messagePart);
    }

    [Fact]
    public async Task CreateSavingsBox_RecordsCreatedEvent()
    {
        var s = await CreateScenarioAsync();

        var boxId = await CreateBoxAsync(s, "  Viagem  ");

        var events = await GetEventsAsync(s.Client);
        var e = events.Should().ContainSingle().Subject;
        e.GetProperty("kind").GetString().Should().Be("Created");
        e.GetProperty("accountId").GetGuid().Should().Be(boxId);
        e.GetProperty("boxName").GetString().Should().Be("Viagem");
        e.GetProperty("reason").ValueKind.Should().Be(JsonValueKind.Null);
        e.GetProperty("amount").GetDecimal().Should().Be(0m);
        e.GetProperty("destinationAccountId").ValueKind.Should().Be(JsonValueKind.Null);
        e.GetProperty("destinationName").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Delete_WithBalance_MovesEverythingToDestinationAndRecordsEvent()
    {
        var s = await CreateScenarioAsync();
        var origin = await CreateBoxAsync(s, "Viagem");
        var destination = await CreateBoxAsync(s, "Reserva");
        await DepositAsync(s, origin, 120m);
        await DepositAsync(s, destination, 30m);

        var response = await DeleteBoxAsync(s.Client, origin,
            new { reason = "  Plano cancelado  ", destinationAccountId = destination });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var accounts = await GetAccountsAsync(s.Client);
        accounts.Should().NotContain(a => a.GetProperty("id").GetGuid() == origin,
            "a caixinha excluída some das contas ativas");
        accounts.Single(a => a.GetProperty("id").GetGuid() == destination)
            .GetProperty("balance").GetDecimal().Should().Be(150m);

        var events = await GetEventsAsync(s.Client);
        events.Should().HaveCount(3);
        var deleted = events.Single(e => e.GetProperty("kind").GetString() == "Deleted");
        deleted.GetProperty("accountId").GetGuid().Should().Be(origin);
        deleted.GetProperty("boxName").GetString().Should().Be("Viagem");
        deleted.GetProperty("reason").GetString().Should().Be("Plano cancelado");
        deleted.GetProperty("amount").GetDecimal().Should().Be(120m);
        deleted.GetProperty("destinationAccountId").GetGuid().Should().Be(destination);
        deleted.GetProperty("destinationName").GetString().Should().Be("Reserva");
    }

    [Fact]
    public async Task Delete_WithBalance_OriginBalanceBecomesZeroAndMovementsAreSavingsTransactions()
    {
        var s = await CreateScenarioAsync();
        var origin = await CreateBoxAsync(s, "Viagem");
        var destination = await CreateBoxAsync(s, "Reserva");
        await DepositAsync(s, origin, 80m);

        (await DeleteBoxAsync(s.Client, origin,
            new { reason = "Teste", destinationAccountId = destination }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var now = DateTime.UtcNow;
        var originRows = await GetDataAsync(await s.Client.GetAsync(
            $"/api/accounts/{origin}/transactions?month={now.Month}&year={now.Year}"));
        var destinationRows = await GetDataAsync(await s.Client.GetAsync(
            $"/api/accounts/{destination}/transactions?month={now.Month}&year={now.Year}"));

        originRows.EnumerateArray().Sum(t => t.GetProperty("type").GetString() == "Income"
                ? t.GetProperty("amount").GetDecimal()
                : -t.GetProperty("amount").GetDecimal())
            .Should().Be(0m);
        var received = destinationRows.EnumerateArray().Should().ContainSingle().Subject;
        received.GetProperty("amount").GetDecimal().Should().Be(80m);
        received.GetProperty("type").GetString().Should().Be("Income");
        received.GetProperty("paymentMethod").GetString().Should().Be("Savings");
        received.GetProperty("observations").GetString().Should().Be("Teste");
        received.GetProperty("description").GetString().Should().ContainEquivalentOf("Viagem");
    }

    [Fact]
    public async Task Delete_WithZeroBalance_NeedsNoDestinationAndIgnoresOne()
    {
        var s = await CreateScenarioAsync();
        var withoutDestination = await CreateBoxAsync(s, "Vazia 1");
        var withIgnoredDestination = await CreateBoxAsync(s, "Vazia 2");

        (await DeleteBoxAsync(s.Client, withoutDestination, new { reason = "Não uso mais" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await DeleteBoxAsync(s.Client, withIgnoredDestination,
            new { reason = "Não uso mais", destinationAccountId = Guid.NewGuid() }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var accounts = await GetAccountsAsync(s.Client);
        accounts.Should().NotContain(a => a.GetProperty("id").GetGuid() == withoutDestination);
        accounts.Should().NotContain(a => a.GetProperty("id").GetGuid() == withIgnoredDestination);

        var deleted = (await GetEventsAsync(s.Client))
            .Where(e => e.GetProperty("kind").GetString() == "Deleted").ToList();
        deleted.Should().HaveCount(2);
        deleted.Should().OnlyContain(e => e.GetProperty("amount").GetDecimal() == 0m);
        deleted.Should().OnlyContain(e => e.GetProperty("destinationAccountId").ValueKind == JsonValueKind.Null);
        deleted.Should().OnlyContain(e => e.GetProperty("destinationName").ValueKind == JsonValueKind.Null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Delete_WithoutReason_ReturnsBadRequestAndKeepsBox(string? reason)
    {
        var s = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");

        var response = await DeleteBoxAsync(s.Client, box, new { reason });

        await ShouldBeBadRequestAsync(response, "motivo");
        (await GetAccountsAsync(s.Client)).Should().Contain(a => a.GetProperty("id").GetGuid() == box);
        (await GetEventsAsync(s.Client)).Should().OnlyContain(e => e.GetProperty("kind").GetString() == "Created");
    }

    [Fact]
    public async Task Delete_WithReasonLongerThanLimit_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");

        var response = await DeleteBoxAsync(s.Client, box, new { reason = new string('a', 201) });

        await ShouldBeBadRequestAsync(response, "no máximo");
    }

    [Fact]
    public async Task Delete_WithBalanceAndNoDestination_ReturnsBadRequestAndMovesNothing()
    {
        var s = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");
        await DepositAsync(s, box, 50m);

        var response = await DeleteBoxAsync(s.Client, box, new { reason = "Teste" });

        await ShouldBeBadRequestAsync(response, "destino");
        (await GetAccountsAsync(s.Client)).Single(a => a.GetProperty("id").GetGuid() == box)
            .GetProperty("balance").GetDecimal().Should().Be(50m);
    }

    [Fact]
    public async Task Delete_WithBalanceAndDestinationEqualToItself_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");
        await DepositAsync(s, box, 50m);

        var response = await DeleteBoxAsync(s.Client, box,
            new { reason = "Teste", destinationAccountId = box });

        await ShouldBeBadRequestAsync(response, "diferente");
        (await GetAccountsAsync(s.Client)).Single(a => a.GetProperty("id").GetGuid() == box)
            .GetProperty("balance").GetDecimal().Should().Be(50m);
    }

    [Fact]
    public async Task Delete_WithBalanceAndNonexistentDestination_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");
        await DepositAsync(s, box, 50m);

        var response = await DeleteBoxAsync(s.Client, box,
            new { reason = "Teste", destinationAccountId = Guid.NewGuid() });

        await ShouldBeBadRequestAsync(response, "destino");
    }

    [Fact]
    public async Task Delete_WithBalanceAndCheckingAccountDestination_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");
        await DepositAsync(s, box, 50m);

        var response = await DeleteBoxAsync(s.Client, box,
            new { reason = "Teste", destinationAccountId = s.CheckingId });

        await ShouldBeBadRequestAsync(response, "destino");
        (await GetAccountsAsync(s.Client)).Should().Contain(a => a.GetProperty("id").GetGuid() == box);
    }

    [Fact]
    public async Task Delete_WithBalanceAndDestinationFromOtherHousehold_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();
        var other = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");
        var foreignBox = await CreateBoxAsync(other, "Alheia");
        await DepositAsync(s, box, 50m);

        var response = await DeleteBoxAsync(s.Client, box,
            new { reason = "Teste", destinationAccountId = foreignBox });

        await ShouldBeBadRequestAsync(response, "destino");
        (await GetAccountsAsync(other.Client)).Single(a => a.GetProperty("id").GetGuid() == foreignBox)
            .GetProperty("balance").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task Delete_WithBalanceAndDeletedDestination_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");
        var gone = await CreateBoxAsync(s, "Antiga");
        await DepositAsync(s, box, 50m);
        (await DeleteBoxAsync(s.Client, gone, new { reason = "Teste" })).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await DeleteBoxAsync(s.Client, box,
            new { reason = "Teste", destinationAccountId = gone });

        await ShouldBeBadRequestAsync(response, "destino");
    }

    [Fact]
    public async Task Delete_BoxFromOtherHousehold_ReturnsBadRequestAndLeavesItUntouched()
    {
        var owner = await CreateScenarioAsync();
        var intruder = await CreateScenarioAsync();
        var box = await CreateBoxAsync(owner, "Viagem");

        var response = await DeleteBoxAsync(intruder.Client, box, new { reason = "Teste" });

        await ShouldBeBadRequestAsync(response, "não pertence");
        (await GetAccountsAsync(owner.Client)).Should().Contain(a => a.GetProperty("id").GetGuid() == box);
        (await GetEventsAsync(owner.Client)).Should().OnlyContain(e => e.GetProperty("kind").GetString() == "Created");
    }

    [Fact]
    public async Task Delete_CheckingAccountOrUnknownId_ReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();

        await ShouldBeBadRequestAsync(
            await DeleteBoxAsync(s.Client, s.CheckingId, new { reason = "Teste" }), "caixinha");
        await ShouldBeBadRequestAsync(
            await DeleteBoxAsync(s.Client, Guid.NewGuid(), new { reason = "Teste" }), "não encontrada");
    }

    [Fact]
    public async Task GenericAccountDelete_OnSavingsBox_ReturnsBadRequestAndKeepsBox()
    {
        var s = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");

        var response = await s.Client.DeleteAsync($"/api/accounts/{box}");

        await ShouldBeBadRequestAsync(response, "motivo");
        (await GetEventsAsync(s.Client)).Count(e => e.GetProperty("kind").GetString() == "Deleted")
            .Should().Be(0);
    }

    [Fact]
    public async Task Delete_SameBoxTwice_SecondCallReturnsBadRequest()
    {
        var s = await CreateScenarioAsync();
        var box = await CreateBoxAsync(s, "Viagem");

        (await DeleteBoxAsync(s.Client, box, new { reason = "Teste" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await DeleteBoxAsync(s.Client, box, new { reason = "Teste" });

        await ShouldBeBadRequestAsync(second, "excluída");
        (await GetEventsAsync(s.Client)).Count(e => e.GetProperty("kind").GetString() == "Deleted")
            .Should().Be(1);
    }

    [Fact]
    public async Task GetEvents_ListsOnlyOwnHouseholdMostRecentFirst()
    {
        var a = await CreateScenarioAsync();
        var b = await CreateScenarioAsync();
        var first = await CreateBoxAsync(a, "Primeira");
        await CreateBoxAsync(b, "De outro lar");
        var second = await CreateBoxAsync(a, "Segunda");
        (await DeleteBoxAsync(a.Client, first, new { reason = "Teste" })).StatusCode.Should().Be(HttpStatusCode.OK);

        var events = await GetEventsAsync(a.Client);

        events.Should().HaveCount(3);
        events.Select(e => e.GetProperty("boxName").GetString())
            .Should().NotContain("De outro lar");
        events[0].GetProperty("kind").GetString().Should().Be("Deleted");
        events[0].GetProperty("accountId").GetGuid().Should().Be(first);
        events[1].GetProperty("accountId").GetGuid().Should().Be(second);
        events[2].GetProperty("accountId").GetGuid().Should().Be(first);

        (await GetEventsAsync(b.Client)).Should().ContainSingle()
            .Which.GetProperty("boxName").GetString().Should().Be("De outro lar");
    }
}
