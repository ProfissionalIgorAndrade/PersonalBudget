using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.IsActive)
            .IsRequired();

        // 🔹 Type enum
        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.HouseholdId)
            .IsRequired();

        builder.HasIndex(c => new { c.HouseholdId, c.Name });


    }
}
