using PersonalBudget.Application.DTOs.Budget;

public class CategoryBudgetService : ICategoryBudgetService
{
    private readonly ICategoryBudgetRepository _repository;
    private readonly ITransactionQueryRepository _txQuery;

    public CategoryBudgetService(
        ICategoryBudgetRepository repository,
        ITransactionQueryRepository txQuery)
    {
        _repository = repository;
        _txQuery = txQuery;
    }

    public async Task<Guid> UpsertAsync(UpsertCategoryBudgetCommand command)
    {
        var existing = await _repository.GetByCategoryAndMonthAsync(
            command.HouseholdId, command.CategoryId, command.Month, command.Year);

        if (existing is not null)
        {
            existing.UpdateLimit(command.LimitAmount);
            await _repository.UpdateAsync(existing);
            return existing.Id;
        }

        var budget = CategoryBudget.Create(
            command.HouseholdId, command.CategoryId, command.Month, command.Year, command.LimitAmount);
        await _repository.AddAsync(budget);
        return budget.Id;
    }

    public async Task DeleteAsync(Guid householdId, Guid budgetId)
    {
        var budget = await _repository.GetByIdAsync(budgetId);

        if (budget is null || budget.HouseholdId != householdId)
            throw new DomainException("Orçamento não encontrado.");

        await _repository.DeleteAsync(budget);
    }

    public async Task<IEnumerable<CategoryBudgetDto>> GetByMonthAsync(Guid householdId, int month, int year)
    {
        var budgets = await _repository.GetByHouseholdAndMonthAsync(householdId, month, year);

        var grouped = await _txQuery.GetGroupedByCategoryForMonthAsync(householdId, month, year);
        var spentByCategory = grouped.Expenses
            .Where(e => e.CategoryId.HasValue)
            .ToDictionary(e => e.CategoryId!.Value, e => e.Total);

        return budgets.Select(b => new CategoryBudgetDto(
            b.Id,
            b.CategoryId,
            b.Month,
            b.Year,
            b.LimitAmount,
            spentByCategory.TryGetValue(b.CategoryId, out var spent) ? spent : 0m
        ));
    }
}
