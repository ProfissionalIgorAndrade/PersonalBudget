using FluentAssertions;

namespace PersonalBudget.Domain.Tests.SavingsBoxes;

public class SavingsBoxEventTests
{
    private static readonly Guid Household = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();
    private static readonly Guid Box = Guid.NewGuid();
    private static readonly Guid Destination = Guid.NewGuid();

    private static SavingsBoxEvent Deleted(
        string? boxName = "Viagem",
        string? reason = "Meta cancelada",
        decimal amount = 150m,
        Guid? destinationId = null,
        string? destinationName = "Reserva",
        bool useDefaultDestination = true) =>
        SavingsBoxEvent.Deleted(Household, User, Box, boxName!, reason, amount,
            useDefaultDestination ? destinationId ?? Destination : destinationId, destinationName);

    private static void ShouldFail(Func<SavingsBoxEvent> act, string messagePart) =>
        act.Should().Throw<DomainException>().Which.Message.Should().ContainEquivalentOf(messagePart);

    [Fact]
    public void Created_SetsFieldsAndNoReasonAmountOrDestination()
    {
        var before = DateTime.UtcNow;
        var e = SavingsBoxEvent.Created(Household, User, Box, "  Viagem  ");

        e.Id.Should().NotBe(Guid.Empty);
        e.HouseholdId.Should().Be(Household);
        e.UserId.Should().Be(User);
        e.AccountId.Should().Be(Box);
        e.BoxName.Should().Be("Viagem");
        e.Kind.Should().Be(SavingsBoxEventKind.Created);
        e.Reason.Should().BeNull();
        e.Amount.Should().Be(0m);
        e.DestinationAccountId.Should().BeNull();
        e.DestinationName.Should().BeNull();
        e.OccurredAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Created_UsesGivenOccurredAt()
    {
        var at = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        SavingsBoxEvent.Created(Household, User, Box, "Viagem", at).OccurredAt.Should().Be(at);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Created_RejectsBlankBoxName(string? name) =>
        ShouldFail(() => SavingsBoxEvent.Created(Household, User, Box, name!), "nome");

    [Fact]
    public void Created_RejectsBoxNameLongerThanLimit() =>
        ShouldFail(() => SavingsBoxEvent.Created(Household, User, Box, new string('a', Account.NameMaxLength + 1)),
            "no máximo");

    [Fact]
    public void Created_AcceptsBoxNameAtLimit()
    {
        var e = SavingsBoxEvent.Created(Household, User, Box, new string('a', Account.NameMaxLength));

        e.BoxName.Should().HaveLength(Account.NameMaxLength);
    }

    [Fact]
    public void Created_RejectsEmptyIds()
    {
        ShouldFail(() => SavingsBoxEvent.Created(Guid.Empty, User, Box, "Viagem"), "lar");
        ShouldFail(() => SavingsBoxEvent.Created(Household, Guid.Empty, Box, "Viagem"), "Usuário");
        ShouldFail(() => SavingsBoxEvent.Created(Household, User, Guid.Empty, "Viagem"), "Caixinha");
    }

    [Fact]
    public void Deleted_SetsFieldsAndTrimsReasonAndNames()
    {
        var e = Deleted(boxName: " Viagem ", reason: "  Meta cancelada  ", destinationName: " Reserva ");

        e.Kind.Should().Be(SavingsBoxEventKind.Deleted);
        e.BoxName.Should().Be("Viagem");
        e.Reason.Should().Be("Meta cancelada");
        e.Amount.Should().Be(150m);
        e.DestinationAccountId.Should().Be(Destination);
        e.DestinationName.Should().Be("Reserva");
    }

    [Fact]
    public void Deleted_WithoutDestination_AllowsZeroAmountAndNullDestination()
    {
        var e = Deleted(amount: 0m, destinationId: null, destinationName: null, useDefaultDestination: false);

        e.Amount.Should().Be(0m);
        e.DestinationAccountId.Should().BeNull();
        e.DestinationName.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deleted_RequiresReason(string? reason) =>
        ShouldFail(() => Deleted(reason: reason), "motivo");

    [Fact]
    public void Deleted_RejectsReasonLongerThanLimit() =>
        ShouldFail(() => Deleted(reason: new string('a', SavingsBoxEvent.ReasonMaxLength + 1)), "no máximo");

    [Fact]
    public void Deleted_AcceptsReasonAtLimit()
    {
        var e = Deleted(reason: new string('a', SavingsBoxEvent.ReasonMaxLength));

        e.Reason.Should().HaveLength(SavingsBoxEvent.ReasonMaxLength);
    }

    [Fact]
    public void Deleted_RejectsNegativeAmount() =>
        ShouldFail(() => Deleted(amount: -1m), "negativo");

    [Fact]
    public void Deleted_WithDestinationId_RequiresDestinationName() =>
        ShouldFail(() => Deleted(destinationName: "  "), "destino");

    [Fact]
    public void Deleted_RejectsEmptyDestinationId() =>
        ShouldFail(() => Deleted(destinationId: Guid.Empty, useDefaultDestination: false), "destino");
}
