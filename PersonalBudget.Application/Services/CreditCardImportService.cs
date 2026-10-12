using System.Globalization;
using System.Text.RegularExpressions;
using PersonalBudget.Application.Interfaces;

public class CreditCardImportService : ICreditCardImportService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] DateFormats = { "yyyy-MM-dd", "dd/MM/yyyy" };

    private readonly ITransactionService _transactionService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICreditCardStatementRepository _statementRepository;

    public CreditCardImportService(
        ITransactionService transactionService,
        ITransactionRepository transactionRepository,
        ICreditCardRepository creditCardRepository,
        ICreditCardStatementRepository statementRepository)
    {
        _transactionService = transactionService;
        _transactionRepository = transactionRepository;
        _creditCardRepository = creditCardRepository;
        _statementRepository = statementRepository;
    }

    /// <summary>
    /// Importa linhas de CSV para uma fatura (cartão + mês/ano) sem duplicar o que já existe.
    /// Uma linha é considerada existente quando o id_sistema é de um lançamento do lar ou,
    /// na falta dele, quando data + valor + tipo + descrição batem com um lançamento da fatura.
    /// </summary>
    public async Task<ImportStatementResult> ImportStatementAsync(ImportStatementCommand command)
    {
        if (command.StatementMonth < 1 || command.StatementMonth > 12)
            throw new DomainException("StatementMonth deve estar entre 1 e 12.");

        var creditCard = await _creditCardRepository.GetByIdAsync(command.CreditCardId);
        if (creditCard is null || creditCard.HouseholdId != command.HouseholdId)
            throw new DomainException("Cartão de crédito não encontrado.");

        var rows = command.Rows ?? [];

        var statement = await _statementRepository.GetByCreditCardAndMonthYearAsync(
            creditCard.Id, command.StatementMonth, command.StatementYear);
        var statementTransactions = statement is null
            ? []
            : (await _transactionRepository.GetByStatementIdAsync(statement.Id)).ToList();

        var rowIds = rows
            .Select(r => Guid.TryParse(r.ExternalId, out var id) ? id : (Guid?)null)
            .ToList();
        var knownIds = await GetHouseholdTransactionIdsAsync(rowIds, command.HouseholdId);

        // Cada lançamento da fatura casa com no máximo uma linha do CSV. Os que já foram
        // reconhecidos por id ficam de fora, para não "absorverem" uma segunda linha igual.
        var unmatchedByContent = new Dictionary<string, int>();
        foreach (var t in statementTransactions.Where(t => !knownIds.Contains(t.Id)))
        {
            var key = ContentKey(t.Date.Value, t.Amount.Amount, t.Type, t.Description.Value);
            unmatchedByContent[key] = unmatchedByContent.GetValueOrDefault(key) + 1;
        }

        int created = 0, skipped = 0;
        var errors = new List<string>();
        var results = new List<ImportStatementRowResult>(rows.Count);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            if (rowIds[i] is { } id && knownIds.Contains(id))
            {
                skipped++;
                results.Add(new ImportStatementRowResult(i, ImportStatementRowStatus.Existing, null));
                continue;
            }

            var contentKey = TryParseDate(row.Date, out var date)
                ? ContentKey(date, row.Amount, row.Type, row.Description)
                : null;
            if (contentKey is not null && unmatchedByContent.GetValueOrDefault(contentKey) > 0)
            {
                unmatchedByContent[contentKey]--;
                skipped++;
                results.Add(new ImportStatementRowResult(i, ImportStatementRowStatus.Existing, null));
                continue;
            }

            if (command.DryRun)
            {
                results.Add(new ImportStatementRowResult(i, ImportStatementRowStatus.New, null));
                continue;
            }

            try
            {
                await _transactionService.CreateAsync(BuildCreateCommand(command, row));
                created++;
                results.Add(new ImportStatementRowResult(i, ImportStatementRowStatus.New, null));
            }
            catch (Exception ex)
            {
                errors.Add($"Linha {i + 1}: \"{row.Description}\": {ex.Message}");
                results.Add(new ImportStatementRowResult(i, ImportStatementRowStatus.Error, ex.Message));
            }
        }

        return new ImportStatementResult(created, skipped, errors, results);
    }

    private async Task<HashSet<Guid>> GetHouseholdTransactionIdsAsync(
        IEnumerable<Guid?> rowIds, Guid householdId)
    {
        var ids = rowIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var existing = await _transactionRepository.GetByIdsAsync(ids);
        return existing
            .Where(t => t.HouseholdId == householdId)
            .Select(t => t.Id)
            .ToHashSet();
    }

    private static CreateTransactionCommand BuildCreateCommand(
        ImportStatementCommand command, ImportStatementRowRequest row) =>
        new(
            UserId: command.UserId,
            HouseholdId: command.HouseholdId,
            AttributionProfileId: row.AttributionProfileId,
            AccountId: null,
            CategoryId: row.CategoryId,
            CreditCardId: command.CreditCardId,
            FromAccountId: null,
            ToAccountId: null,
            Type: row.Type,
            Frequency: TransactionFrequency.Variable,
            PaymentMethod: PaymentMethod.CreditCard,
            Amount: row.Amount,
            Date: row.Date,
            Description: row.Description,
            InstallmentCount: null,
            TotalAmount: null,
            Title: null,
            StatementMonth: command.StatementMonth,
            StatementYear: command.StatementYear,
            Observations: string.IsNullOrWhiteSpace(row.Observations) ? null : row.Observations
        );

    private static bool TryParseDate(string? value, out DateTime date) =>
        DateTime.TryParseExact(value?.Trim(), DateFormats, PtBr, DateTimeStyles.None, out date);

    private static string ContentKey(DateTime date, decimal amount, TransactionType type, string? description)
    {
        var normalized = Regex.Replace((description ?? "").Trim(), @"\s+", " ").ToLowerInvariant();
        var value = Math.Round(amount, 2).ToString("0.00", CultureInfo.InvariantCulture);
        return $"{date:yyyy-MM-dd}|{value}|{(int)type}|{normalized}";
    }
}
