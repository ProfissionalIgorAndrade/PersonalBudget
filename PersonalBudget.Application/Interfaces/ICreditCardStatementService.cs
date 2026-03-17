using PersonalBudget.Application.DTOs.CreditCard;

namespace PersonalBudget.Application.Interfaces;

public interface ICreditCardStatementService
{
    Task<List<CreditCardStatementDto>> GetByCreditCardAsync(Guid creditCardId);

    Task<StatementWithTransactionsResponse?> GetStatementWithTransactionsAsync(Guid userId, Guid creditCardId, int month, int year);

    Task CloseAsync(CloseStatementCommand command);

    Task PayAsync(PayStatementCommand command);
}