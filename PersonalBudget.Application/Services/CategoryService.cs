public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> CreateAsync(CreateCategoryCommand command)
    {
        var category = Category.Create(command.UserId, command.Name, command.Type);
        await _repository.AddAsync(category);
        return category.Id;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(Guid userId)
        => await _repository.GetByUserAsync(userId);

    public async Task UpdateAsync(UpdateCategoryCommand command)
    {
        var category = await _repository.GetByIdAsync(command.CategoryId);

        if (category is null || category.UserId != command.UserId)
            throw new DomainException("Category not found.");

        category.Rename(command.Name, command.Type);
        await _repository.UpdateAsync(category);
    }

    public async Task DeleteAsync(DeleteCategoryCommand command)
    {
        var category = await _repository.GetByIdAsync(command.CategoryId);

        if (category is null || category.UserId != command.UserId)
            throw new DomainException("Category not found.");

        category.Deactivate();
        await _repository.UpdateAsync(category);
    }
}
