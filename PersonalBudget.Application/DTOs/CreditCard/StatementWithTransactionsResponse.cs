namespace PersonalBudget.Application.DTOs.CreditCard;

/// <summary>
/// Fatura do cartão com todos os lançamentos e informações de limite, fechamento e vencimento.
/// A data de vencimento é calculada: se dia de vencimento >= dia de fechamento, é no mesmo mês;
/// caso contrário, é no mês seguinte (ex.: fechamento 30/mar, vencimento dia 8 → 8/abr).
/// </summary>
public record StatementWithTransactionsResponse(
    Guid StatementId,
    Guid CreditCardId,
    string CreditCardName,
    decimal Limit,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime ClosingDate,
    DateTime DueDate,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<StatementTransactionItemDto> Transactions
);
