namespace PersonalBudget.Application.DTOs.CreditCard;

/// <summary>
/// Fatura do cartão com todos os lançamentos e informações de limite e vencimento.
/// A data de vencimento é calculada: dia de vencimento do cartão no mês da fatura,
/// limitado ao último dia do mês (ex.: dia 31 em fevereiro vence dia 28 ou 29).
/// </summary>
public record StatementWithTransactionsResponse(
    Guid StatementId,
    Guid CreditCardId,
    string CreditCardName,
    decimal Limit,
    DateTime DueDate,
    decimal TotalAmount,
    IReadOnlyList<StatementTransactionItemDto> Transactions
);
