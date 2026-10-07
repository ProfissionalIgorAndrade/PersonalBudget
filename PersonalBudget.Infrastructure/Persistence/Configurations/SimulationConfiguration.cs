using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SimulationConfiguration : IEntityTypeConfiguration<Simulation>
{
    public void Configure(EntityTypeBuilder<Simulation> builder)
    {
        builder.ToTable("simulations");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.HouseholdId)
            .HasColumnName("household_id")
            .IsRequired();

        builder.Property(s => s.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(s => s.Description)
            .HasColumnName("description")
            .IsRequired()
            .HasMaxLength(SimulationRules.MaxDescriptionLength);

        builder.Property(s => s.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.Mode)
            .HasColumnName("mode")
            .IsRequired()
            .HasConversion<int>();

        // "yyyy-MM"
        builder.Property(s => s.StartMonth)
            .HasColumnName("start_month")
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(s => s.Amount)
            .HasColumnName("amount")
            .IsRequired();

        builder.Property(s => s.AmountKind)
            .HasColumnName("amount_kind")
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.Installments)
            .HasColumnName("installments");

        builder.Property(s => s.Months)
            .HasColumnName("months");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(s => s.HouseholdId);
        builder.HasIndex(s => s.OwnerUserId);
    }
}
