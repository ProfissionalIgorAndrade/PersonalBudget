namespace PersonalBudget.Application.DTOs.CreditCard;

public record PaginatedStatementWithTransactionsResponse(
    Guid StatementId,
    Guid CreditCardId,
    string CreditCardName,
    decimal Limit,
    DateTime DueDate,
    decimal TotalAmount,
    IReadOnlyList<StatementTransactionItemDto> Transactions,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
