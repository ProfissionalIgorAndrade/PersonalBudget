using PersonalBudget.Application.DTOs.CreditCard;

public interface ITransactionQueryRepository
{
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByUserAsync(Guid userId);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(Guid userId, int month, int year);
    Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByUserAndMonthPagedAsync(
        Guid userId, int month, int year, int page, int pageSize);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(Guid userId, Guid accountId, int month, int year);
    Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByAccountAndMonthPagedAsync(
        Guid userId, Guid accountId, int month, int year, int page, int pageSize);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetAllTransactionByCreditCardStatementAndMonthYearQuery(Guid userId, Guid creditCardId, int month, int year);
    Task<IReadOnlyList<StatementTransactionItemDto>> GetTransactionDetailsByStatementIdAsync(Guid statementId);
    Task<(IReadOnlyList<StatementTransactionItemDto> Items, int TotalCount)> GetTransactionDetailsByStatementIdPagedAsync(
        Guid statementId, int page, int pageSize);
}
