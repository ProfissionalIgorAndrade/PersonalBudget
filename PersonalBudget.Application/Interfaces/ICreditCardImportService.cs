namespace PersonalBudget.Application.Interfaces;

public interface ICreditCardImportService
{
    Task<ImportStatementResult> ImportStatementAsync(ImportStatementCommand command);
}
