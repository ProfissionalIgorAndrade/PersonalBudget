using PersonalBudget.Application.DTOs.CreditCard;

namespace PersonalBudget.Application.Interfaces;

public interface ICreditCardStatementService
{
    Task<List<CreditCardStatementDto>> GetByCreditCardAsync(Guid creditCardId);

    Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsAsync(Guid householdId, Guid creditCardId, int month, int year);
    Task<PaginatedStatementWithTransactionsResponse?> GetStatementWithTransactionsPagedAsync(
        Guid householdId, Guid creditCardId, int month, int year, int page, int pageSize);

    Task CloseAsync(CloseStatementCommand command);

    Task PayAsync(PayStatementCommand command);
}
