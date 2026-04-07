public record PaginatedTransactionsResult(
    IReadOnlyList<GetAllTransactionByUserResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal PeriodTotalIncome,
    decimal PeriodTotalExpense)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
