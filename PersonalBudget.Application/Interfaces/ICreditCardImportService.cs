namespace PersonalBudget.Application.Interfaces;

public interface ICreditCardImportService
{
    Task<ImportCreditCardCsvResult> ImportAsync(ImportCreditCardCsvCommand command);
}
