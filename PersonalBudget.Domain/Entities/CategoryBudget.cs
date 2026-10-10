
public class CategoryBudget
{
    public Guid Id { get; private set; }
    public Guid HouseholdId { get; private set; }
    public Guid CategoryId { get; private set; }
    public int Month { get; private set; }
    public int Year { get; private set; }
    public decimal LimitAmount { get; private set; }

    public CategoryBudget(Guid householdId, Guid categoryId, int month, int year, decimal limitAmount)
    {
        if (householdId == Guid.Empty)
            throw new DomainException("Budget deve pertencer a um lar.");
        if (categoryId == Guid.Empty)
            throw new DomainException("Budget deve ter uma categoria.");
        if (limitAmount <= 0)
            throw new DomainException("Limite deve ser maior que zero.");

        Id = Guid.NewGuid();
        HouseholdId = householdId;
        CategoryId = categoryId;
        Month = month;
        Year = year;
        LimitAmount = limitAmount;
    }

    protected CategoryBudget() { }

    public static CategoryBudget Create(Guid householdId, Guid categoryId, int month, int year, decimal limitAmount)
        => new(householdId, categoryId, month, year, limitAmount);

    public void UpdateLimit(decimal limitAmount)
    {
        if (limitAmount <= 0)
            throw new DomainException("Limite deve ser maior que zero.");
        LimitAmount = limitAmount;
    }
}
