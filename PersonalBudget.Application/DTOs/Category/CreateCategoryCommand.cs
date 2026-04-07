public record CreateCategoryCommand(Guid HouseholdId, string Name, CategoryType Type, string? Color = null, string? Icon = null);
