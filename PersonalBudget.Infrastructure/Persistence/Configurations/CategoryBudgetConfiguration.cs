using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CategoryBudgetConfiguration : IEntityTypeConfiguration<CategoryBudget>
{
    public void Configure(EntityTypeBuilder<CategoryBudget> builder)
    {
        builder.ToTable("category_budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.HouseholdId).IsRequired();
        builder.Property(b => b.CategoryId).IsRequired();
        builder.Property(b => b.Month).IsRequired();
        builder.Property(b => b.Year).IsRequired();
        builder.Property(b => b.LimitAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(b => new { b.HouseholdId, b.CategoryId, b.Month, b.Year }).IsUnique();
    }
}
