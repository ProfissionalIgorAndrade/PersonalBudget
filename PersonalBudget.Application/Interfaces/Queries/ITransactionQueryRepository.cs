using PersonalBudget.Application.DTOs.CreditCard;
using PersonalBudget.Application.DTOs.Household;

public interface ITransactionQueryRepository
{
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByHouseholdAsync(Guid householdId);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByHouseholdAndMonthAsync(Guid householdId, int month, int year);
    Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByHouseholdAndMonthPagedAsync(
        Guid householdId, int month, int year, int page, int pageSize);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(Guid householdId, Guid accountId, int month, int year);
    Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByAccountAndMonthPagedAsync(
        Guid householdId, Guid accountId, int month, int year, int page, int pageSize);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetAllTransactionByCreditCardStatementAndMonthYearQuery(Guid householdId, Guid creditCardId, int month, int year);
    Task<IReadOnlyList<StatementTransactionItemDto>> GetTransactionDetailsByStatementIdAsync(Guid statementId);
    Task<(IReadOnlyList<StatementTransactionItemDto> Items, int TotalCount)> GetTransactionDetailsByStatementIdPagedAsync(
        Guid statementId, int page, int pageSize);
    /// <summary>Total líquido da fatura: soma de despesas menos receitas (reembolsos/estornos).</summary>
    Task<decimal> GetStatementNetTotalAsync(Guid statementId);

    /// <summary>Resumo por correspondente: todos os perfis do lar; totais 0 quando não há lançamentos no mês.</summary>
    Task<IReadOnlyList<HouseholdProfileSummaryRow>> GetHouseholdSummaryByProfileAsync(Guid householdId, int month, int year);
}
