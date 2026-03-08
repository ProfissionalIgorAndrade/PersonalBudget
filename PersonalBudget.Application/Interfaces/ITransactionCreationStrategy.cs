namespace PersonalBudget.Application.Interfaces;

public interface ITransactionCreationStrategy
{
    PaymentMethod PaymentMethod { get; }
    Task<Guid> CreateAsync(CreateTransactionCommand command);
}