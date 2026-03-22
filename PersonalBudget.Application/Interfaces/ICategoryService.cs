public interface ICategoryService
{
    Task<Guid> CreateAsync(CreateCategoryCommand command);
    Task<IEnumerable<Category>> GetAllAsync(Guid householdId);
    Task UpdateAsync(UpdateCategoryCommand command);
    Task DeleteAsync(DeleteCategoryCommand command);
}
