
public class Category
{
    public Category(Guid userId, string name, bool isSystem, CategoryType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        Id = Guid.NewGuid();
        UserId = userId;
        Name = name;
        IsSystem = isSystem;
        Type = type;
        IsActive = true;
    }

    protected Category() { }

    public Guid Id { get; set; }
    public Guid UserId { get; private set; }
    public string Name { get; set; }
    public CategoryType Type { get; set; }
    public bool IsSystem { get; }
    public bool IsActive { get; private set; }

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