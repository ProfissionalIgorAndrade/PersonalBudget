/// <summary>
/// Classificação da recorrência / forma da transação.
/// Variável: lançamento pontual. Fixa: recorrente (ex.: salário no dia 10).
/// Parcelas: compra parcelada no cartão (fatura); exige InstallmentCount &gt; 1.
/// </summary>
public enum TransactionFrequency
{
    Variable = 1,
    Fixed = 2,
    Installments = 3
}
