using PersonalBudget.Application.DTOs.CreditCard;

namespace PersonalBudget.Application.Interfaces;

public interface ICreditCardStatementService
{
    Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsAsync(Guid householdId, Guid creditCardId, int month, int year);
    Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsByIdAsync(Guid householdId, Guid creditCardId, Guid statementId);
    Task<PaginatedStatementWithTransactionsResponse?> GetStatementWithTransactionsPagedAsync(
        Guid householdId, Guid creditCardId, int month, int year, int page, int pageSize);
    Task<PaginatedStatementWithTransactionsResponse?> GetStatementWithTransactionsByIdPagedAsync(
        Guid householdId, Guid creditCardId, Guid statementId, int page, int pageSize);
    /// <summary>Marca/desmarca como revisados todos os lançamentos da fatura.</summary>
    Task SetReviewedAsync(Guid householdId, Guid creditCardId, Guid statementId, bool reviewed);
}
