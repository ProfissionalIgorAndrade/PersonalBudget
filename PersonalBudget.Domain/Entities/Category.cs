
public class Category
{
    public Guid Id { get; set; }
    public Guid UserId { get; private set; }
    public string Name { get; set; }
    public CategoryType Type { get; set; }
    public bool IsSystem { get; }
    public bool IsActive { get; private set; }

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

    public static bool Exists(string name, List<Category> categories) => categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static Category Create(Guid userId, string name, CategoryType type)
           => new(userId, name, false, type);

    public void Rename(string name, CategoryType type)
    {
        if (!IsActive)
            throw new DomainException("Inactive category cannot be updated.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        Name = name.Trim();
        Type = type;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Category already inactive.");

        IsActive = false;
    }
}