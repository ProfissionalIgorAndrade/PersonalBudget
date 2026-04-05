using System.Globalization;
using PersonalBudget.Application.Interfaces;

public class CreditCardImportService : ICreditCardImportService
{
    private readonly ITransactionService _transactionService;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IHouseholdMemberProfileRepository _profileRepository;

    public CreditCardImportService(
        ITransactionService transactionService,
        ICreditCardRepository creditCardRepository,
        ICategoryRepository categoryRepository,
        IHouseholdMemberProfileRepository profileRepository)
    {
        _transactionService = transactionService;
        _creditCardRepository = creditCardRepository;
        _categoryRepository = categoryRepository;
        _profileRepository = profileRepository;
    }

    public async Task<ImportCreditCardCsvResult> ImportAsync(ImportCreditCardCsvCommand command)
    {
        var creditCard = await _creditCardRepository.GetByIdAsync(command.CreditCardId);
        if (creditCard is null || creditCard.HouseholdId != command.HouseholdId)
            throw new DomainException("Cartão de crédito não encontrado.");

        var categories = (await _categoryRepository.GetByHouseholdAsync(command.HouseholdId)).ToList();
        var profiles = await _profileRepository.GetByHouseholdAsync(command.HouseholdId);

        var rows = ParseCsv(command.CsvContent);

        int imported = 0, skipped = 0;
        var errors = new List<string>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var label = $"Linha {i + 2}: \"{row.Descricao}\"";
            try
            {
                var profileId = ResolveProfile(row.Pessoa, profiles);
                if (profileId is null)
                {
                    errors.Add($"{label}: perfil '{row.Pessoa}' não encontrado no lar.");
                    skipped++;
                    continue;
                }

                var categoryId = await ResolveOrCreateCategoryAsync(row.Categoria, command.HouseholdId, categories);
                var status = MapStatus(row.Status);

                await CreateTransactionsAsync(command, row, profileId.Value, categoryId, status);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"{label}: {ex.Message}");
                skipped++;
            }
        }

        return new ImportCreditCardCsvResult(imported, skipped, errors);
    }

    private async Task CreateTransactionsAsync(
        ImportCreditCardCsvCommand command,
        CsvRow row,
        Guid profileId,
        Guid? categoryId,
        TransactionStatus status)
    {
        var type = row.ValorParcela < 0 ? TransactionType.Income : TransactionType.Expense;
        var amount = Math.Abs(row.ValorParcela);
        var date = row.Data;

        // Each CSV row represents exactly one charge on the statement — always create a single transaction.
        // For installment rows, include the parcel context in the description so the info is preserved.
        var description = row.TotalParcelas > 1
            ? $"{row.Descricao} ({row.ParcelaAtual}/{row.TotalParcelas})"
            : row.Descricao;

        var cmd = new CreateTransactionCommand(
            UserId: command.UserId,
            HouseholdId: command.HouseholdId,
            AttributionProfileId: profileId,
            AccountId: null,
            CategoryId: categoryId,
            CreditCardId: command.CreditCardId,
            FromAccountId: null,
            ToAccountId: null,
            Type: type,
            Frequency: TransactionFrequency.Variable,
            PaymentMethod: PaymentMethod.CreditCard,
            Amount: amount,
            Date: date,
            Description: description,
            AutoComplete: false,
            InstallmentCount: null,
            TotalAmount: null,
            Title: null,
            Status: status
        );
        await _transactionService.CreateAsync(cmd);
    }

    private static Guid? ResolveProfile(string? pessoa, IReadOnlyList<HouseholdMemberProfile> profiles)
    {
        if (string.IsNullOrWhiteSpace(pessoa))
            return profiles.FirstOrDefault()?.Id;

        return profiles
            .FirstOrDefault(p => p.DisplayName.Equals(pessoa.Trim(), StringComparison.OrdinalIgnoreCase))
            ?.Id;
    }

    private async Task<Guid?> ResolveOrCreateCategoryAsync(
        string? name,
        Guid householdId,
        List<Category> categories)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var existing = categories.FirstOrDefault(c =>
            c.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            return existing.Id;

        var newCat = Category.Create(householdId, name.Trim(), CategoryType.Expense);
        await _categoryRepository.AddAsync(newCat);
        await _categoryRepository.SaveChangesAsync();
        categories.Add(newCat);
        return newCat.Id;
    }

    private static TransactionStatus MapStatus(string? rawStatus) =>
        rawStatus?.Trim().ToLowerInvariant() switch
        {
            "pago" => TransactionStatus.Completed,
            "estornado" => TransactionStatus.Cancelled,
            _ => TransactionStatus.Pending
        };

    // ── CSV parsing ──────────────────────────────────────────────────────────

    private static List<CsvRow> ParseCsv(string csvContent)
    {
        var lines = csvContent
            .ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
            return [];

        // Build header index map from first line
        var headers = lines[0]
            .Split(',')
            .Select((h, i) => (key: h.Trim().ToLowerInvariant(), idx: i))
            .ToDictionary(x => x.key, x => x.idx);

        var rows = new List<CsvRow>(lines.Length - 1);
        for (var i = 1; i < lines.Length; i++)
        {
            var fields = SplitCsvLine(lines[i]);
            if (fields.Length < 7) continue; // skip malformed/empty lines

            string Get(string col) =>
                headers.TryGetValue(col, out var idx) && idx < fields.Length
                    ? fields[idx].Trim()
                    : "";

            // status and observacoes are always the last two columns; handle rows
            // that may have extra empty columns in the middle (e.g. extra blank column)
            var status = fields.Length >= 2 ? fields[^2].Trim() : "";
            var observacoes = fields.Length >= 1 ? fields[^1].Trim() : "";

            rows.Add(new CsvRow(
                Descricao: Get("descricao"),
                Data: Get("data"),
                ValorParcela: ParseDecimal(Get("valor_parcela")),
                ParcelaAtual: ParseInt(Get("parcela_atual")),
                TotalParcelas: ParseInt(Get("total_parcelas")),
                ValorTotalCompra: ParseDecimal(Get("valor_total_compra")),
                Pessoa: Get("pessoa"),
                Categoria: Get("categoria"),
                Status: status,
                Observacoes: observacoes
            ));
        }

        return rows;
    }

    private static string[] SplitCsvLine(string line)
    {
        // Simple CSV split respecting double-quoted fields
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    private static int ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 1;
        return int.TryParse(value, out var n) ? n : 1;
    }

    private record CsvRow(
        string Descricao,
        string Data,
        decimal ValorParcela,
        int ParcelaAtual,
        int TotalParcelas,
        decimal ValorTotalCompra,
        string Pessoa,
        string Categoria,
        string Status,
        string Observacoes
    );
}
