public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> CreateAsync(CreateCategoryCommand command)
    {
        var category = Category.Create(command.HouseholdId, command.Name, command.Type);
        await _repository.AddAsync(category);
        return category.Id;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(Guid householdId)
        => await _repository.GetByHouseholdAsync(householdId);

    public async Task UpdateAsync(UpdateCategoryCommand command)
    {
        var category = await _repository.GetByIdAsync(command.CategoryId);

        if (category is null || category.HouseholdId != command.HouseholdId)
            throw new DomainException("Categoria não encontrada.");

        category.Rename(command.Name, command.Type);
        await _repository.UpdateAsync(category);
    }

    public async Task DeleteAsync(DeleteCategoryCommand command)
    {
        var category = await _repository.GetByIdAsync(command.CategoryId);

        if (category is null || category.HouseholdId != command.HouseholdId)
            throw new DomainException("Categoria não encontrada.");

        category.Deactivate();
        await _repository.UpdateAsync(category);
    }
}
