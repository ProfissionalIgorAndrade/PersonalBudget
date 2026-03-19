namespace PersonalBudget.Application.DTOs.CreditCard;

public record PaginatedStatementWithTransactionsResponse(
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
    IReadOnlyList<StatementTransactionItemDto> Transactions,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
