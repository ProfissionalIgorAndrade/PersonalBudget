using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalBudget.Integration.Tests.Helpers;

namespace PersonalBudget.Integration.Tests.Tests;

/// <summary>
/// api/simulations: simulações salvas no servidor e compartilhadas pelo lar. Todos os usuários de
/// teste se chamam "Test User", então as asserções de dono usam ownerUserId e isOwner, nunca o nome.
/// </summary>
public class SimulationsTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    private const string NotFound = "Simulação não encontrada.";
    private const string NotOwner = "Simulação não pertence ao usuário.";

    private sealed record TestUser(HttpClient Client, string Email, Guid HouseholdId);

    private async Task<TestUser> CreateUserAsync()
    {
        var client = factory.CreateClient();
        var email = $"sim_{Guid.NewGuid():N}@test.com";
        client.SetBearer(await AuthHelper.SignInAsync(client, email));

        var households = await GetDataAsync(await client.GetAsync("/api/households"));
        return new TestUser(client, email, households[0].GetProperty("id").GetGuid());
    }

    /// <summary>Dois usuários no mesmo lar: o convidado aceita o convite do dono.</summary>
    private async Task<(TestUser Owner, TestUser Member)> CreateHouseholdPairAsync()
    {
        var owner = await CreateUserAsync();
        var member = await CreateUserAsync();
        await JoinAsync(owner, member);
        return (owner, member);
    }

    private static async Task JoinAsync(TestUser owner, TestUser invitee)
    {
        var invite = await owner.Client.PostAsJsonAsync("/api/households/invites",
            new { householdId = owner.HouseholdId, inviteeEmail = invitee.Email });
        invite.StatusCode.Should().Be(HttpStatusCode.OK, await invite.Content.ReadAsStringAsync());
        var token = (await invite.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("token").GetString();

        var accept = await invitee.Client.PostAsJsonAsync("/api/households/invites/accept", new { token });
        accept.StatusCode.Should().Be(HttpStatusCode.OK, await accept.Content.ReadAsStringAsync());
    }

    private static async Task<JsonElement> GetDataAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"unexpected response: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data");
    }

    private static object Body(
        string? description = "Carro",
        string type = "Expense",
        string mode = "Single",
        string startMonth = "2026-11",
        decimal amount = 100m,
        string? amountKind = null,
        int? installments = null,
        int? months = null) =>
        new { description, type, mode, startMonth, amount, amountKind, installments, months };

    private static async Task<Guid> CreateAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/simulations", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task<List<JsonElement>> ListAsync(HttpClient client)
    {
        var data = await GetDataAsync(await client.GetAsync("/api/simulations"));
        return data.EnumerateArray().ToList();
    }

    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("message").GetString()!;
    }

    private static List<object> Items(int count, string prefix = "Item") =>
        Enumerable.Range(1, count).Select(i => Body(description: $"{prefix} {i}")).ToList();

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/simulations");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_NewUser_ReturnsEmptyList()
    {
        var u = await CreateUserAsync();
        (await ListAsync(u.Client)).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAndItemAppearsInList()
    {
        var u = await CreateUserAsync();
        var id = await CreateAsync(u.Client, Body(description: "  Notebook  ", mode: "Installment",
            amount: 3600m, amountKind: "Total", installments: 12));

        var item = (await ListAsync(u.Client)).Should().ContainSingle().Subject;
        item.GetProperty("id").GetGuid().Should().Be(id);
        item.GetProperty("description").GetString().Should().Be("Notebook");
        item.GetProperty("type").GetString().Should().Be("Expense");
        item.GetProperty("mode").GetString().Should().Be("Installment");
        item.GetProperty("startMonth").GetString().Should().Be("2026-11");
        item.GetProperty("amount").GetDecimal().Should().Be(3600m);
        item.GetProperty("amountKind").GetString().Should().Be("Total");
        item.GetProperty("installments").GetInt32().Should().Be(12);
        item.GetProperty("months").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("isOwner").GetBoolean().Should().BeTrue();
        item.GetProperty("ownerUserId").GetGuid().Should().NotBe(Guid.Empty);
        item.GetProperty("ownerName").GetString().Should().NotBeNullOrWhiteSpace();
        item.GetProperty("createdAt").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
        item.GetProperty("updatedAt").GetDateTime().Should().Be(item.GetProperty("createdAt").GetDateTime());
    }

    [Fact]
    public async Task Create_WithoutAmountKind_DefaultsToPerInstallment()
    {
        var u = await CreateUserAsync();
        await CreateAsync(u.Client, Body(amountKind: null));

        (await ListAsync(u.Client)).Single().GetProperty("amountKind").GetString().Should().Be("PerInstallment");
    }

    [Fact]
    public async Task Create_Monthly_WithZeroMonths_StoresNull_AndDropsInstallments()
    {
        var u = await CreateUserAsync();
        await CreateAsync(u.Client, Body(type: "Income", mode: "Monthly", months: 0, installments: 4));

        var item = (await ListAsync(u.Client)).Single();
        item.GetProperty("months").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("installments").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("type").GetString().Should().Be("Income");
    }

    [Fact]
    public async Task Create_Monthly_WithMonths_KeepsMonths()
    {
        var u = await CreateUserAsync();
        await CreateAsync(u.Client, Body(mode: "Monthly", months: 6));

        (await ListAsync(u.Client)).Single().GetProperty("months").GetInt32().Should().Be(6);
    }

    [Fact]
    public async Task GetAll_IsOrderedByCreation()
    {
        var u = await CreateUserAsync();
        var first = await CreateAsync(u.Client, Body(description: "A"));
        var second = await CreateAsync(u.Client, Body(description: "B"));
        var third = await CreateAsync(u.Client, Body(description: "C"));

        (await ListAsync(u.Client)).Select(i => i.GetProperty("id").GetGuid())
            .Should().Equal(first, second, third);
    }

    [Fact]
    public async Task Update_ByOwner_ChangesFields()
    {
        var u = await CreateUserAsync();
        var id = await CreateAsync(u.Client, Body(mode: "Installment", installments: 3));

        var response = await u.Client.PutAsJsonAsync($"/api/simulations/{id}",
            Body(description: "Salário", type: "Income", mode: "Monthly", startMonth: "2027-01", amount: 5000m, months: 6));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var item = (await ListAsync(u.Client)).Single();
        item.GetProperty("id").GetGuid().Should().Be(id);
        item.GetProperty("description").GetString().Should().Be("Salário");
        item.GetProperty("type").GetString().Should().Be("Income");
        item.GetProperty("mode").GetString().Should().Be("Monthly");
        item.GetProperty("startMonth").GetString().Should().Be("2027-01");
        item.GetProperty("amount").GetDecimal().Should().Be(5000m);
        item.GetProperty("installments").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("months").GetInt32().Should().Be(6);
        item.GetProperty("updatedAt").GetDateTime().Should().BeOnOrAfter(item.GetProperty("createdAt").GetDateTime());
    }

    [Fact]
    public async Task Update_Unknown_ReturnsBadRequestNotFound()
    {
        var u = await CreateUserAsync();

        var response = await u.Client.PutAsJsonAsync($"/api/simulations/{Guid.NewGuid()}", Body());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Be(NotFound);
    }

    [Fact]
    public async Task Delete_ByOwner_RemovesIt()
    {
        var u = await CreateUserAsync();
        var id = await CreateAsync(u.Client, Body());

        var response = await u.Client.DeleteAsync($"/api/simulations/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ListAsync(u.Client)).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_Unknown_ReturnsBadRequestNotFound()
    {
        var u = await CreateUserAsync();

        var response = await u.Client.DeleteAsync($"/api/simulations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Be(NotFound);
    }

    [Fact]
    public async Task Household_MemberSeesOwnersSimulation_WithOwnerNameAndIsOwner()
    {
        var (owner, member) = await CreateHouseholdPairAsync();
        var id = await CreateAsync(owner.Client, Body(description: "Casa"));

        var ownerView = (await ListAsync(owner.Client)).Single();
        var memberView = (await ListAsync(member.Client)).Single();

        ownerView.GetProperty("isOwner").GetBoolean().Should().BeTrue();
        memberView.GetProperty("isOwner").GetBoolean().Should().BeFalse();
        memberView.GetProperty("id").GetGuid().Should().Be(id);
        memberView.GetProperty("ownerUserId").GetGuid().Should().Be(ownerView.GetProperty("ownerUserId").GetGuid());
        memberView.GetProperty("ownerName").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Household_OwnerSeesMembersSimulation_AsNotOwner()
    {
        var (owner, member) = await CreateHouseholdPairAsync();
        await CreateAsync(owner.Client, Body(description: "Do dono"));
        await CreateAsync(member.Client, Body(description: "Do membro"));

        var ownerView = await ListAsync(owner.Client);
        var memberView = await ListAsync(member.Client);

        ownerView.Should().HaveCount(2);
        memberView.Should().HaveCount(2);
        ownerView.Single(i => i.GetProperty("description").GetString() == "Do dono")
            .GetProperty("isOwner").GetBoolean().Should().BeTrue();
        ownerView.Single(i => i.GetProperty("description").GetString() == "Do membro")
            .GetProperty("isOwner").GetBoolean().Should().BeFalse();
        memberView.Single(i => i.GetProperty("description").GetString() == "Do membro")
            .GetProperty("isOwner").GetBoolean().Should().BeTrue();

        var ownerIds = ownerView.Select(i => i.GetProperty("ownerUserId").GetGuid()).Distinct().ToList();
        ownerIds.Should().HaveCount(2, "cada simulação carrega o id do seu dono, mesmo com nomes iguais");
    }

    [Fact]
    public async Task Household_NonOwnerUpdate_ReturnsForbidden_AndKeepsData()
    {
        var (owner, member) = await CreateHouseholdPairAsync();
        var id = await CreateAsync(owner.Client, Body(description: "Original"));

        var response = await member.Client.PutAsJsonAsync($"/api/simulations/{id}", Body(description: "Alterada"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorMessageAsync(response)).Should().Be(NotOwner);
        (await ListAsync(owner.Client)).Single().GetProperty("description").GetString().Should().Be("Original");
    }

    [Fact]
    public async Task Household_NonOwnerDelete_ReturnsForbidden_AndKeepsData()
    {
        var (owner, member) = await CreateHouseholdPairAsync();
        var id = await CreateAsync(owner.Client, Body());

        var response = await member.Client.DeleteAsync($"/api/simulations/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorMessageAsync(response)).Should().Be(NotOwner);
        (await ListAsync(owner.Client)).Should().ContainSingle();
    }

    [Fact]
    public async Task OtherHousehold_SeesNothing_AndCannotTouchIt()
    {
        var a = await CreateUserAsync();
        var b = await CreateUserAsync();
        var id = await CreateAsync(a.Client, Body());

        (await ListAsync(b.Client)).Should().BeEmpty();

        var put = await b.Client.PutAsJsonAsync($"/api/simulations/{id}", Body(description: "X"));
        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(put)).Should().Be(NotFound);

        var delete = await b.Client.DeleteAsync($"/api/simulations/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(delete)).Should().Be(NotFound);

        (await ListAsync(a.Client)).Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteMine_RemovesOnlyMine()
    {
        var (owner, member) = await CreateHouseholdPairAsync();
        await CreateAsync(owner.Client, Body(description: "Dono 1"));
        await CreateAsync(owner.Client, Body(description: "Dono 2"));
        await CreateAsync(member.Client, Body(description: "Membro 1"));

        var response = await owner.Client.DeleteAsync("/api/simulations/mine");

        var data = await GetDataAsync(response);
        data.GetProperty("removed").GetInt32().Should().Be(2);
        var left = await ListAsync(member.Client);
        left.Should().ContainSingle();
        left[0].GetProperty("description").GetString().Should().Be("Membro 1");
    }

    [Fact]
    public async Task DeleteMine_WithNothing_RemovesZero()
    {
        var u = await CreateUserAsync();

        var data = await GetDataAsync(await u.Client.DeleteAsync("/api/simulations/mine"));

        data.GetProperty("removed").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Import_CreatesAllForTheCaller_InReceivedOrder()
    {
        var u = await CreateUserAsync();

        var response = await u.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = Items(5) });

        var data = await GetDataAsync(response);
        data.GetProperty("imported").GetInt32().Should().Be(5);
        data.GetProperty("ids").GetArrayLength().Should().Be(5);

        var list = await ListAsync(u.Client);
        list.Select(i => i.GetProperty("description").GetString())
            .Should().Equal("Item 1", "Item 2", "Item 3", "Item 4", "Item 5");
        list.Should().OnlyContain(i => i.GetProperty("isOwner").GetBoolean());
    }

    [Fact]
    public async Task Import_WithOneInvalidItem_FailsAndImportsNothing()
    {
        var u = await CreateUserAsync();
        var items = Items(3);
        items[1] = Body(description: "Ruim", amount: 0m);

        var response = await u.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = items });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Be("Simulação #2 (\"Ruim\"): o valor deve ser maior que zero.");
        (await ListAsync(u.Client)).Should().BeEmpty();
    }

    [Fact]
    public async Task Import_WithNullItem_FailsAndImportsNothing()
    {
        var u = await CreateUserAsync();
        var items = new List<object?> { Body(), null };

        var response = await u.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = items });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Contain("Simulação #2");
        (await ListAsync(u.Client)).Should().BeEmpty();
    }

    [Fact]
    public async Task Import_Empty_ReturnsBadRequest()
    {
        var u = await CreateUserAsync();

        var response = await u.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = Items(0) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Import_FiftyOne_ExceedsCap_AndImportsNothing()
    {
        var u = await CreateUserAsync();

        var response = await u.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = Items(51) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ListAsync(u.Client)).Should().BeEmpty();
    }

    [Fact]
    public async Task Import_Fifty_IsAccepted_ThenCreateFails()
    {
        var u = await CreateUserAsync();

        var import = await u.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = Items(50) });
        (await GetDataAsync(import)).GetProperty("imported").GetInt32().Should().Be(50);

        var create = await u.Client.PostAsJsonAsync("/api/simulations", Body());
        create.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(create)).Should().Contain("50");
        (await ListAsync(u.Client)).Should().HaveCount(50);
    }

    [Fact]
    public async Task Import_CountsExistingOnes_AgainstTheCap()
    {
        var u = await CreateUserAsync();
        await u.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = Items(30) });

        var response = await u.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = Items(21, "Extra") });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ListAsync(u.Client)).Should().HaveCount(30);

        var ok = await u.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = Items(20, "Extra") });
        ok.StatusCode.Should().Be(HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());
        (await ListAsync(u.Client)).Should().HaveCount(50);
    }

    [Fact]
    public async Task Cap_IsPerOwner_NotPerHousehold()
    {
        var (owner, member) = await CreateHouseholdPairAsync();
        await owner.Client.PostAsJsonAsync("/api/simulations/import", new { simulations = Items(50) });

        var response = await member.Client.PostAsJsonAsync("/api/simulations", Body(description: "Do membro"));

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        (await ListAsync(owner.Client)).Should().HaveCount(51);
    }

    public static IEnumerable<object[]> InvalidBodies()
    {
        yield return new object[] { Body(description: new string('a', 121)), "no máximo 120" };
        yield return new object[] { Body(startMonth: "2026-13"), "mês de início inválido" };
        yield return new object[] { Body(startMonth: "2026-1"), "mês de início inválido" };
        yield return new object[] { Body(startMonth: "1999-12"), "mês de início inválido" };
        yield return new object[] { Body(startMonth: "2101-01"), "mês de início inválido" };
        yield return new object[] { Body(amount: 0m), "maior que zero" };
        yield return new object[] { Body(amount: -10m), "maior que zero" };
        yield return new object[] { Body(mode: "Installment"), "número de parcelas" };
        yield return new object[] { Body(mode: "Installment", installments: 0), "número de parcelas" };
        yield return new object[] { Body(mode: "Installment", installments: 121), "número de parcelas" };
        yield return new object[] { Body(mode: "Monthly", months: 121), "duração mensal" };
        yield return new object[] { Body(mode: "Monthly", months: -1), "duração mensal" };
        yield return new object[] { new { description = "Sem tipo", mode = "Single", startMonth = "2026-11", amount = 10m }, "tipo inválido" };
        yield return new object[] { new { description = "Sem modo", type = "Expense", startMonth = "2026-11", amount = 10m }, "modo inválido" };
        yield return new object[] { new { description = "Sem mês", type = "Expense", mode = "Single", amount = 10m }, "mês de início inválido" };
    }

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Create_InvalidBody_ReturnsBadRequest(object body, string messagePart)
    {
        var u = await CreateUserAsync();

        var response = await u.Client.PostAsJsonAsync("/api/simulations", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Contain(messagePart).And.StartWith("Simulação");
        (await ListAsync(u.Client)).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Update_InvalidBody_ReturnsBadRequest_AndKeepsData(object body, string messagePart)
    {
        var u = await CreateUserAsync();
        var id = await CreateAsync(u.Client, Body(description: "Original"));

        var response = await u.Client.PutAsJsonAsync($"/api/simulations/{id}", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Contain(messagePart);
        (await ListAsync(u.Client)).Single().GetProperty("description").GetString().Should().Be("Original");
    }

    [Fact]
    public async Task Create_WithUnknownEnumName_ReturnsBadRequest()
    {
        var u = await CreateUserAsync();

        var response = await u.Client.PostAsJsonAsync("/api/simulations", Body(type: "Banana"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithoutBody_ReturnsBadRequest()
    {
        var u = await CreateUserAsync();

        var response = await u.Client.PostAsync("/api/simulations", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task AcceptInvite_MovesInviteesSimulations_ToTargetHousehold_KeepingOwnership()
    {
        var owner = await CreateUserAsync();
        var invitee = await CreateUserAsync();
        await CreateAsync(owner.Client, Body(description: "Do dono"));
        await CreateAsync(invitee.Client, Body(description: "Convidado 1"));
        await CreateAsync(invitee.Client, Body(description: "Convidado 2", mode: "Installment", installments: 6));

        var inviteeBefore = await ListAsync(invitee.Client);
        inviteeBefore.Should().HaveCount(2);
        var inviteeUserId = inviteeBefore[0].GetProperty("ownerUserId").GetGuid();
        var inviteeIds = inviteeBefore.Select(i => i.GetProperty("id").GetGuid()).ToList();

        await JoinAsync(owner, invitee);

        var inviteeAfter = await ListAsync(invitee.Client);
        inviteeAfter.Should().HaveCount(3);
        var mine = inviteeAfter.Where(i => i.GetProperty("isOwner").GetBoolean()).ToList();
        mine.Select(i => i.GetProperty("id").GetGuid()).Should().BeEquivalentTo(inviteeIds);
        mine.Should().OnlyContain(i => i.GetProperty("ownerUserId").GetGuid() == inviteeUserId);
        inviteeAfter.Single(i => !i.GetProperty("isOwner").GetBoolean())
            .GetProperty("description").GetString().Should().Be("Do dono");

        var ownerView = await ListAsync(owner.Client);
        ownerView.Should().HaveCount(3);
        ownerView.Count(i => i.GetProperty("isOwner").GetBoolean()).Should().Be(1);
        ownerView.Where(i => !i.GetProperty("isOwner").GetBoolean())
            .Should().OnlyContain(i => i.GetProperty("ownerUserId").GetGuid() == inviteeUserId);

        var edit = await invitee.Client.PutAsJsonAsync($"/api/simulations/{inviteeIds[0]}", Body(description: "Editada"));
        edit.StatusCode.Should().Be(HttpStatusCode.OK, await edit.Content.ReadAsStringAsync());
    }
}
