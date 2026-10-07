using FluentAssertions;
using PersonalBudget.Application.DTOs.Simulations;
using PersonalBudget.Application.Services;

namespace PersonalBudget.Domain.Tests.Simulations;

public class SimulationValidatorTests
{
    private static SimulationInput Input(
        string? description = "Carro",
        SimulationType type = SimulationType.Expense,
        SimulationMode mode = SimulationMode.Single,
        string? startMonth = "2026-11",
        decimal amount = 100m,
        SimulationAmountKind? kind = null,
        int? installments = null,
        int? months = null) =>
        new(description, type, mode, startMonth, amount, kind, installments, months);

    private static void ShouldFail(SimulationInput? input, int? number, string expectedMessage)
    {
        Action act = () => SimulationValidator.Validate(input, number);
        act.Should().Throw<DomainException>().Which.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public void ValidInput_DoesNotThrow()
    {
        Action act = () => SimulationValidator.Validate(
            Input(mode: SimulationMode.Installment, installments: 12, kind: SimulationAmountKind.Total));
        act.Should().NotThrow();
    }

    [Fact]
    public void NullInput_Fails_WithNumber()
        => ShouldFail(null, 3, "Simulação #3: valor nulo.");

    [Fact]
    public void Message_NamesNumberAndDescription()
        => ShouldFail(Input(description: "Viagem", amount: 0m), 2,
            "Simulação #2 (\"Viagem\"): o valor deve ser maior que zero.");

    [Fact]
    public void Message_WithoutNumber_NamesOnlyDescription()
        => ShouldFail(Input(description: "Viagem", amount: 0m), null,
            "Simulação (\"Viagem\"): o valor deve ser maior que zero.");

    [Fact]
    public void Message_WithEmptyDescription_NamesOnlyNumber()
        => ShouldFail(Input(description: "  ", amount: -1m), 1,
            "Simulação #1: o valor deve ser maior que zero.");

    [Fact]
    public void Message_TruncatesLongDescriptionInLabel()
    {
        var description = new string('a', 121);
        ShouldFail(Input(description: description), 4,
            $"Simulação #4 (\"{new string('a', 40)}...\"): a descrição deve ter no máximo 120 caracteres.");
    }

    [Fact]
    public void InvalidType_Fails()
        => ShouldFail(Input(type: (SimulationType)0), 1, "Simulação #1 (\"Carro\"): tipo inválido. Use Income ou Expense.");

    [Fact]
    public void InvalidMode_Fails()
        => ShouldFail(Input(mode: (SimulationMode)8), 1, "Simulação #1 (\"Carro\"): modo inválido. Use Single, Installment ou Monthly.");

    [Fact]
    public void InvalidStartMonth_Fails()
        => ShouldFail(Input(startMonth: "2026-13"), 1, "Simulação #1 (\"Carro\"): mês de início inválido. Use o formato yyyy-MM (ex.: 2026-10).");

    [Fact]
    public void StartMonthOutOfYearRange_Fails()
        => ShouldFail(Input(startMonth: "2101-01"), 1, "Simulação #1 (\"Carro\"): mês de início inválido. Use o formato yyyy-MM (ex.: 2026-10).");

    [Fact]
    public void InvalidAmountKind_Fails()
        => ShouldFail(Input(kind: (SimulationAmountKind)5), 1, "Simulação #1 (\"Carro\"): tipo de valor inválido. Use PerInstallment ou Total.");

    [Fact]
    public void OmittedAmountKind_IsAccepted()
    {
        Action act = () => SimulationValidator.Validate(Input(kind: null));
        act.Should().NotThrow();
    }

    [Fact]
    public void InstallmentWithoutInstallments_Fails()
        => ShouldFail(Input(mode: SimulationMode.Installment), 1,
            "Simulação #1 (\"Carro\"): o número de parcelas deve estar entre 1 e 120.");

    [Fact]
    public void InstallmentAbove120_Fails()
        => ShouldFail(Input(mode: SimulationMode.Installment, installments: 121), 1,
            "Simulação #1 (\"Carro\"): o número de parcelas deve estar entre 1 e 120.");

    [Fact]
    public void MonthlyAbove120_Fails()
        => ShouldFail(Input(mode: SimulationMode.Monthly, months: 121), 1,
            "Simulação #1 (\"Carro\"): a duração mensal deve estar entre 0 (até o fim do horizonte) e 120 meses.");

    [Fact]
    public void InstallmentsAreIgnoredOutsideInstallmentMode()
    {
        Action act = () => SimulationValidator.Validate(Input(mode: SimulationMode.Single, installments: 999, months: 999));
        act.Should().NotThrow();
    }

    [Fact]
    public void Cap_IsFiftyPerOwner()
        => SimulationRules.MaxPerOwner.Should().Be(50);
}
