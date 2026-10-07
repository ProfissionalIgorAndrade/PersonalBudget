/// <summary>Forma como a simulação se repete no tempo. Persistido como inteiro: não reordene os valores.</summary>
public enum SimulationMode
{
    /// <summary>Uma única vez, no mês de início.</summary>
    Single = 1,
    /// <summary>Parcelada, em <see cref="Simulation.Installments"/> meses.</summary>
    Installment = 2,
    /// <summary>Todo mês, por <see cref="Simulation.Months"/> meses (nulo = até o fim do horizonte).</summary>
    Monthly = 3
}
