public class CreditCardService : ICreditCardService
{
    private readonly ICreditCardRepository _repository;

    public CreditCardService(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> CreateAsync(CreateCreditCardCommand command)
    {
        var creditCard = CreditCard.Create(
            command.UserId,
            command.HouseholdId,
            command.Name,
            command.Limit,
            command.DueDay,
            command.Color,
            command.MemberId
        );

        await _repository.AddAsync(creditCard);
        return creditCard.Id;
    }

    public async Task<IEnumerable<CreditCard>> GetAllAsync(Guid householdId)
    {
        return await _repository.GetByHouseholdAsync(householdId);
    }

    public async Task UpdateAsync(UpdateCreditCardCommand command)
    {
        var creditCard = await _repository.GetByIdAsync(command.CreditCardId);

        if (creditCard is null || creditCard.HouseholdId != command.HouseholdId)
            throw new DomainException("Cartão de crédito não encontrado.");

        creditCard.Update(
            command.Name,
            command.Limit,
            command.DueDay,
            command.Color,
            command.MemberId
        );

        await _repository.UpdateAsync(creditCard);
    }

    public async Task DeleteAsync(DeleteCreditCardCommand command)
    {
        var creditCard = await _repository.GetByIdAsync(command.CreditCardId);

        if (creditCard is null || creditCard.HouseholdId != command.HouseholdId)
            throw new DomainException("Cartão de crédito não encontrado.");

        creditCard.Deactivate();
        await _repository.UpdateAsync(creditCard);
    }
}
