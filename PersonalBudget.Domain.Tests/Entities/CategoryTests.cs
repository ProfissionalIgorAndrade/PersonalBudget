public class CategoryTests
{
    [Fact]
    public void Should_not_create_category_without_name()
    {
        Assert.Throws<DomainException>(() =>
            new Category(Guid.NewGuid(), "", true, CategoryType.Income));
    }

    [Fact]
    public void Should_create_active_category_income_type()
    {
        var category = new Category(Guid.NewGuid(), "Food", true, CategoryType.Income);

        Assert.True(category.IsActive);
        Assert.Equal(CategoryType.Income, category.Type);
    }

    [Fact]
    public void Should_create_active_category_expense_type()
    {
        var category = new Category(Guid.NewGuid(), "Food", true, CategoryType.Expense);

        Assert.True(category.IsActive);
        Assert.Equal(CategoryType.Expense, category.Type);
    }

    [Fact]
    public void Should_deactivate_category()
    {
        var category = new Category(Guid.NewGuid(), "Food", true, CategoryType.Expense);

        category.Deactivate();

        Assert.False(category.IsActive);
    }
}
