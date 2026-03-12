using PersonalBudget.Application.DTOs.CreditCard;

namespace PersonalBudget.Application.Interfaces;

public interface ICreditCardStatementService
{
    Task<List<CreditCardStatementDto>> GetByCreditCardAsync(Guid creditCardId);

    Task CloseAsync(CloseStatementCommand command);

    Task PayAsync(PayStatementCommand command);
}