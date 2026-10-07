using FluentAssertions;

namespace PersonalBudget.Domain.Tests.Simulations;

public class SimulationTests
{
    private static readonly Guid Household = Guid.NewGuid();
    private static readonly Guid Owner = Guid.NewGuid();

    private static Simulation Create(
        string? description = "Carro",
        SimulationType type = SimulationType.Expense,
        SimulationMode mode = SimulationMode.Single,
        string? startMonth = "2026-11",
        decimal amount = 100m,
        SimulationAmountKind kind = SimulationAmountKind.PerInstallment,
        int? installments = null,
        int? months = null) =>
        Simulation.Create(Household, Owner, description, type, mode, startMonth, amount, kind, installments, months);

    private static void ShouldFail(Func<Simulation> act, string messagePart) =>
        act.Should().Throw<DomainException>().Which.Message.Should().Contain(messagePart);

    [Fact]
    public void Create_Single_SetsFieldsAndServerGeneratedId()
    {
        var before = DateTime.UtcNow;
        var s = Create(description: "  Carro  ", installments: 5, months: 7);

        s.Id.Should().NotBe(Guid.Empty);
        s.HouseholdId.Should().Be(Household);
        s.OwnerUserId.Should().Be(Owner);
        s.Description.Should().Be("Carro");
        s.Type.Should().Be(SimulationType.Expense);
        s.Mode.Should().Be(SimulationMode.Single);
        s.StartMonth.Should().Be("2026-11");
        s.Amount.Should().Be(100m);
        s.Installments.Should().BeNull("parcelas só valem no modo Installment");
        s.Months.Should().BeNull("duração só vale no modo Monthly");
        s.CreatedAt.Should().BeOnOrAfter(before);
        s.UpdatedAt.Should().Be(s.CreatedAt);
    }

    [Fact]
    public void Create_GeneratesDistinctIds()
        => Create().Id.Should().NotBe(Create().Id);

    [Fact]
    public void Create_NullDescription_BecomesEmpty()
        => Create(description: null).Description.Should().BeEmpty();

    [Fact]
    public void Create_DescriptionWith120Chars_IsAccepted()
        => Create(description: new string('a', 120)).Description.Should().HaveLength(120);

    [Fact]
    public void Create_DescriptionOver120Chars_Fails()
        => ShouldFail(() => Create(description: new string('a', 121)), "no máximo 120");

    [Fact]
    public void Create_DescriptionIsMeasuredAfterTrim()
        => Create(description: "  " + new string('a', 120) + "  ").Description.Should().HaveLength(120);

    [Fact]
    public void Create_Installment_KeepsInstallments()
    {
        var s = Create(mode: SimulationMode.Installment, installments: 12, kind: SimulationAmountKind.Total, months: 9);

        s.Installments.Should().Be(12);
        s.Months.Should().BeNull();
        s.AmountKind.Should().Be(SimulationAmountKind.Total);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void Create_Installment_WithInvalidInstallments_Fails(int? installments)
        => ShouldFail(() => Create(mode: SimulationMode.Installment, installments: installments), "parcelas");

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    public void Create_Installment_AtLimits_IsAccepted(int installments)
        => Create(mode: SimulationMode.Installment, installments: installments).Installments.Should().Be(installments);

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(1, 1)]
    [InlineData(120, 120)]
    public void Create_Monthly_NormalizesMonths(int? input, int? expected)
        => Create(mode: SimulationMode.Monthly, months: input).Months.Should().Be(expected);

    [Theory]
    [InlineData(-1)]
    [InlineData(121)]
    public void Create_Monthly_WithInvalidMonths_Fails(int months)
        => ShouldFail(() => Create(mode: SimulationMode.Monthly, months: months), "duração mensal");

    [Fact]
    public void Create_Monthly_IgnoresInstallments()
        => Create(mode: SimulationMode.Monthly, installments: 5).Installments.Should().BeNull();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-13")]
    [InlineData("2026-1")]
    [InlineData("2026/10")]
    [InlineData("2026-10-01")]
    [InlineData("1999-12")]
    [InlineData("2101-01")]
    public void Create_WithInvalidStartMonth_Fails(string? startMonth)
        => ShouldFail(() => Create(startMonth: startMonth), "mês de início");

    [Theory]
    [InlineData("2000-01")]
    [InlineData("2100-12")]
    public void Create_StartMonthAtYearLimits_IsAccepted(string startMonth)
        => Create(startMonth: startMonth).StartMonth.Should().Be(startMonth);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveAmount_Fails(int amount)
        => ShouldFail(() => Create(amount: amount), "maior que zero");

    [Fact]
    public void Create_WithUndefinedType_Fails()
        => ShouldFail(() => Create(type: (SimulationType)0), "tipo inválido");

    [Fact]
    public void Create_WithUndefinedMode_Fails()
        => ShouldFail(() => Create(mode: (SimulationMode)9), "modo inválido");

    [Fact]
    public void Create_WithUndefinedAmountKind_Fails()
        => ShouldFail(() => Create(kind: (SimulationAmountKind)7), "tipo de valor");

    [Fact]
    public void Create_WithEmptyHousehold_Fails()
    {
        Action act = () => Simulation.Create(Guid.Empty, Owner, "x", SimulationType.Income, SimulationMode.Single,
            "2026-11", 1m, SimulationAmountKind.PerInstallment, null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithEmptyOwner_Fails()
    {
        Action act = () => Simulation.Create(Household, Guid.Empty, "x", SimulationType.Income, SimulationMode.Single,
            "2026-11", 1m, SimulationAmountKind.PerInstallment, null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithExplicitCreatedAt_UsesIt()
    {
        var at = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var s = Simulation.Create(Household, Owner, "x", SimulationType.Income, SimulationMode.Single,
            "2026-11", 1m, SimulationAmountKind.PerInstallment, null, null, at);

        s.CreatedAt.Should().Be(at);
        s.UpdatedAt.Should().Be(at);
    }

    [Fact]
    public void Update_ReplacesFieldsAndKeepsIdentity()
    {
        var s = Create(mode: SimulationMode.Installment, installments: 3);
        var id = s.Id;
        var createdAt = s.CreatedAt;

        s.Update("Salário", SimulationType.Income, SimulationMode.Monthly, "2027-01", 5000m,
            SimulationAmountKind.PerInstallment, 3, 6);

        s.Id.Should().Be(id);
        s.OwnerUserId.Should().Be(Owner);
        s.HouseholdId.Should().Be(Household);
        s.CreatedAt.Should().Be(createdAt);
        s.UpdatedAt.Should().BeOnOrAfter(createdAt);
        s.Description.Should().Be("Salário");
        s.Type.Should().Be(SimulationType.Income);
        s.Mode.Should().Be(SimulationMode.Monthly);
        s.StartMonth.Should().Be("2027-01");
        s.Amount.Should().Be(5000m);
        s.Installments.Should().BeNull("o modo deixou de ser Installment");
        s.Months.Should().Be(6);
    }

    [Fact]
    public void Update_WithInvalidData_FailsAndKeepsPreviousState()
    {
        var s = Create();

        Action act = () => s.Update("x", SimulationType.Expense, SimulationMode.Single, "2026-11", 0m,
            SimulationAmountKind.PerInstallment, null, null);

        act.Should().Throw<DomainException>();
        s.Description.Should().Be("Carro");
        s.Amount.Should().Be(100m);
    }

    [Fact]
    public void RelocateToHousehold_ChangesHouseholdAndKeepsOwner()
    {
        var s = Create();
        var target = Guid.NewGuid();

        s.RelocateToHousehold(target);

        s.HouseholdId.Should().Be(target);
        s.OwnerUserId.Should().Be(Owner);
    }

    [Fact]
    public void RelocateToHousehold_WithEmptyId_Fails()
    {
        var s = Create();
        Action act = () => s.RelocateToHousehold(Guid.Empty);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReassignOwner_ChangesOwner()
    {
        var s = Create();
        var other = Guid.NewGuid();

        s.ReassignOwner(other);

        s.OwnerUserId.Should().Be(other);
        s.HouseholdId.Should().Be(Household);
    }

    [Fact]
    public void ReassignOwner_WithEmptyId_Fails()
    {
        var s = Create();
        Action act = () => s.ReassignOwner(Guid.Empty);
        act.Should().Throw<DomainException>();
    }
}
