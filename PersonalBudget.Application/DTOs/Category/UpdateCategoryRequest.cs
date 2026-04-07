public record UpdateCategoryRequest(Guid CategoryId, string Name, CategoryType Type, string? Color = null, string? Icon = null);
