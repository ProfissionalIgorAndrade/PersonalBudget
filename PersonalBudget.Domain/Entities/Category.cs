
public class Category
{
    public Category(string name, bool isSystem, CategoryType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        IsSystem = isSystem;
        IsActive = true;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsSystem { get; }
    public bool IsActive { get; private set; }
    public CategoryType Type { get; set; }

    public static bool Exists(string name, List<Category> categories) => categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void Rename(string newName)
    {
        Name = newName;
    }
    public void Deactivate()
    {
        IsActive = false;
    }
}