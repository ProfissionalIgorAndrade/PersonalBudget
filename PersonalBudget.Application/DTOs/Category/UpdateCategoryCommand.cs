public record UpdateCategoryCommand(Guid HouseholdId, Guid CategoryId, string Name, CategoryType Type);
