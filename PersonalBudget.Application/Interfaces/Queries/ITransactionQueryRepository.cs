using PersonalBudget.Application.DTOs.CreditCard;
using PersonalBudget.Application.DTOs.Dashboard;
using PersonalBudget.Application.DTOs.Household;
using PersonalBudget.Application.DTOs.Transaction;

public interface ITransactionQueryRepository
{
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByHouseholdAsync(Guid householdId);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByHouseholdAndMonthAsync(Guid householdId, int month, int year, TransactionFrequency? frequency = null);
    Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount, decimal PeriodTotalIncome, decimal PeriodTotalExpense)> GetByHouseholdAndMonthPagedAsync(
        Guid householdId, int month, int year, int page, int pageSize, TransactionFrequency? frequency = null);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(Guid householdId, Guid accountId, int month, int year, TransactionFrequency? frequency = null);
    Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByAccountAndMonthPagedAsync(
        Guid householdId, Guid accountId, int month, int year, int page, int pageSize, TransactionFrequency? frequency = null);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetAllTransactionByCreditCardStatementAndMonthYearQuery(Guid householdId, Guid creditCardId, int month, int year);
    Task<IReadOnlyList<StatementTransactionItemDto>> GetTransactionDetailsByStatementIdAsync(Guid statementId);
    Task<(IReadOnlyList<StatementTransactionItemDto> Items, int TotalCount)> GetTransactionDetailsByStatementIdPagedAsync(
        Guid statementId, int page, int pageSize);
    /// <summary>Total líquido da fatura: soma de despesas menos receitas (reembolsos/estornos).</summary>
    Task<decimal> GetStatementNetTotalAsync(Guid statementId);

    /// <summary>Resumo por correspondente: todos os perfis do lar; totais 0 quando não há lançamentos no mês.</summary>
    Task<IReadOnlyList<HouseholdProfileSummaryRow>> GetHouseholdSummaryByProfileAsync(Guid householdId, int month, int year);

    /// <summary>Resumo agregado do mês para o dashboard.</summary>
    Task<DashboardSummaryResponse> GetDashboardSummaryAsync(Guid householdId, int month, int year);

    /// <summary>Tendência mensal de N meses terminando em endMonth/endYear.</summary>
    Task<IReadOnlyList<MonthlyTrendResponse>> GetTransactionTrendsAsync(Guid householdId, int months, int endMonth, int endYear);

    /// <summary>Transações agrupadas por categoria para o mês.</summary>
    Task<TransactionsCategoryGroupResponse> GetGroupedByCategoryForMonthAsync(Guid householdId, int month, int year);

    /// <summary>Todos os parcelamentos ativos (não cancelados) do lar para cálculo de grupos.</summary>
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetInstallmentTransactionsAsync(Guid householdId);
}
