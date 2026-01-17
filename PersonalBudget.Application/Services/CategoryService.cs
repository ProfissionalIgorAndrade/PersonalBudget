public class CategoryService
{
    private readonly List<Category> _categories = new();

    public Category Create(string name, CategoryType type)
    {
        if (Category.Exists(name, _categories))
            throw new InvalidOperationException("Category already exists.");

        var category = new Category(name, isSystem: false, type);
        _categories.Add(category);
        return category;
    }

    public IEnumerable<Category> GetAll() => _categories;
}