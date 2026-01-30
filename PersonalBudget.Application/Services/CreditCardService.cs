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
        // 🔒 Garantia de domínio: cartão só pode existir para conta do usuário
        var account = await _accountRepository.GetByIdAsync(command.AccountId);

        if (account is null || account.UserId != command.UserId)
            throw new DomainException("Account not found.");

        var creditCard = CreditCard.Create(
            command.UserId,
            command.AccountId,
            command.Name,
            command.Limit,
            command.ClosingDay,
            command.DueDay
        );

        await _repository.AddAsync(creditCard);
        return creditCard.Id;
    }

    public async Task<IEnumerable<CreditCard>> GetAllAsync(Guid userId)
    {
        return await _repository.GetByUserAsync(userId);
    }

    public async Task UpdateAsync(UpdateCreditCardCommand command)
    {
        var creditCard = await _repository.GetByIdAsync(command.CreditCardId);

        if (creditCard is null || creditCard.UserId != command.UserId)
            throw new DomainException("Credit card not found.");

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

        if (creditCard is null || creditCard.UserId != command.UserId)
            throw new DomainException("Credit card not found.");

        creditCard.Deactivate();
        await _repository.UpdateAsync(creditCard);
    }
}
