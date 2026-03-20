using System.Globalization;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Application.Services.TransactionCreation;

public abstract class TransactionCreationStrategyBase : ITransactionCreationStrategy
{
    protected readonly ITransactionRepository _transactionRepository;
    protected readonly IAccountRepository _accountRepository;
    protected readonly ICreditCardRepository _creditCardRepository;

    protected static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    protected static readonly string[] DateFormatsBr = { "dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss" };
    protected static readonly string[] DateFormatsIso = 
    {
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss.ffffff"
    };

    protected TransactionCreationStrategyBase(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICreditCardRepository creditCardRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _creditCardRepository = creditCardRepository;
    }

    public abstract PaymentMethod PaymentMethod { get; }
    
    public abstract Task<Guid> CreateAsync(CreateTransactionCommand command);

    /// <summary>
    /// Parse de data aceitando Brasil (dd/MM/yyyy) e formatos ISO.
    /// </summary>
    protected DateTime? ParseOptionalExpirationDate(string? expirationDateString)
    {
        if (string.IsNullOrWhiteSpace(expirationDateString))
            return null;

        return ParseDate(expirationDateString).Date;
    }

    protected DateTime ParseDate(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            throw new DomainException("Data é obrigatória.");

        var trimmed = dateString.Trim();

        if (DateTime.TryParseExact(trimmed, DateFormatsBr, PtBr, DateTimeStyles.None, out var dateBr))
            return dateBr;

        if (DateTime.TryParseExact(trimmed, DateFormatsIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateIso))
            return dateIso;

        throw new DomainException("Data inválida. Use dd/MM/yyyy (ex: 02/03/2026) ou ISO (ex: 2026-03-02T12:48:00).");
    }

    protected async Task<Account> GetAccountOrThrowAsync(Guid accountId, Guid userId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account is null || account.UserId != userId)
            throw new DomainException("Conta não encontrada.");

        return account;
    }

}
