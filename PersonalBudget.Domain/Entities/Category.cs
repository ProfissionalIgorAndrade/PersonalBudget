
public class Category
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; private set; }
    public string Name { get; set; }
    public CategoryType Type { get; set; }
    public bool IsSystem { get; }
    public bool IsActive { get; private set; }

    public Category(Guid householdId, string name, bool isSystem, CategoryType type)
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
    }

    protected Category() { }

    public static bool Exists(string name, List<Category> categories) => categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static Category Create(Guid householdId, string name, CategoryType type)
           => new(householdId, name, false, type);

    public void Rename(string name, CategoryType type)
    {
        if (!IsActive)
            throw new DomainException("Categoria inativa não pode ser atualizada.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome da categoria é obrigatório.");

        Name = name.Trim();
        Type = type;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("A categoria já está inativa.");

        IsActive = false;
    }
}