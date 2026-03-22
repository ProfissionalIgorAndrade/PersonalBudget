public interface ICreditCardService
{
    Task<Guid> CreateAsync(CreateCreditCardCommand command);
    Task<IEnumerable<CreditCard>> GetAllAsync(Guid householdId);
    Task UpdateAsync(UpdateCreditCardCommand command);
    Task DeleteAsync(DeleteCreditCardCommand command);
}
