public class CreditCardService : ICreditCardService
{
    private readonly ICreditCardRepository _repository;
    private readonly IAccountRepository _accountRepository;

    public CreditCardService(
        ICreditCardRepository repository,
        IAccountRepository accountRepository)
    {
        _repository = repository;
        _accountRepository = accountRepository;
    }

    public async Task<Guid> CreateAsync(CreateCreditCardCommand command)
    {
        var account = await _accountRepository.GetByIdAsync(command.AccountId);

        if (account is null || account.HouseholdId != command.HouseholdId)
            throw new DomainException("Conta não encontrada.");

        var creditCard = CreditCard.Create(
            command.UserId,
            command.HouseholdId,
            command.AccountId,
            command.Name,
            command.Limit,
            command.ClosingDay,
            command.DueDay
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
            command.ClosingDay,
            command.DueDay
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
