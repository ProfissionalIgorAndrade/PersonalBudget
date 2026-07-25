using PersonalBudget.Application.DTOs.CreditCard;

namespace PersonalBudget.Application.Interfaces;

public interface ICreditCardStatementService
{
    Task<List<CreditCardStatementDto>> GetByCreditCardAsync(Guid creditCardId);

    Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsAsync(Guid householdId, Guid creditCardId, int month, int year);
    Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsByIdAsync(Guid householdId, Guid creditCardId, Guid statementId);
    Task<PaginatedStatementWithTransactionsResponse?> GetStatementWithTransactionsPagedAsync(
        Guid householdId, Guid creditCardId, int month, int year, int page, int pageSize);
    Task<PaginatedStatementWithTransactionsResponse?> GetStatementWithTransactionsByIdPagedAsync(
        Guid householdId, Guid creditCardId, Guid statementId, int page, int pageSize);

    Task UpdateStatusAsync(UpdateStatementStatusCommand command);
}
