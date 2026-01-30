public interface ICategoryService
{
    Task<Guid> CreateAsync(CreateCategoryCommand command);
    Task<IEnumerable<Category>> GetAllAsync(Guid userId);
    Task UpdateAsync(UpdateCategoryCommand command);
    Task DeleteAsync(DeleteCategoryCommand command);
}
