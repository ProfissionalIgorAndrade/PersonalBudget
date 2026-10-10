using PersonalBudget.Application.DTOs.Budget;

public interface ICategoryBudgetService
{
    Task<Guid> UpsertAsync(UpsertCategoryBudgetCommand command);
    Task DeleteAsync(Guid householdId, Guid budgetId);
    Task<IEnumerable<CategoryBudgetDto>> GetByMonthAsync(Guid householdId, int month, int year);
}
