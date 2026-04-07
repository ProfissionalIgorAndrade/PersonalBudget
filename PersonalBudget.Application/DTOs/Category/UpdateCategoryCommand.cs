public record UpdateCategoryCommand(Guid HouseholdId, Guid CategoryId, string Name, CategoryType Type, string? Color = null, string? Icon = null);
