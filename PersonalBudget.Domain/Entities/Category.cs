
public class Category
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; private set; }
    public string Name { get; set; }
    public CategoryType Type { get; set; }
    public bool IsSystem { get; }
    public bool IsActive { get; private set; }
    /// <summary>CSS hex color, e.g. "#6366f1". Optional display hint for UI rendering.</summary>
    public string? Color { get; private set; }
    /// <summary>Emoji or icon name, e.g. "🏠". Optional display hint for UI rendering.</summary>
    public string? Icon { get; private set; }

    public Category(Guid householdId, string name, bool isSystem, CategoryType type, string? color = null, string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome da categoria é obrigatório.");
        if (householdId == Guid.Empty)
            throw new DomainException("Categoria deve pertencer a um lar.");

        Id = Guid.NewGuid();
        HouseholdId = householdId;
        Name = name;
        IsSystem = isSystem;
        Type = type;
        IsActive = true;
        Color = color;
        Icon = icon;
    }

    protected Category() { }

    public static bool Exists(string name, List<Category> categories) => categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static Category Create(Guid householdId, string name, CategoryType type, string? color = null, string? icon = null)
           => new(householdId, name, false, type, color, icon);

    public void Rename(string name, CategoryType type, string? color = null, string? icon = null)
    {
        if (!IsActive)
            throw new DomainException("Categoria inativa não pode ser atualizada.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome da categoria é obrigatório.");

        Name = name.Trim();
        Type = type;
        Color = color;
        Icon = icon;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("A categoria já está inativa.");

        IsActive = false;
    }
}