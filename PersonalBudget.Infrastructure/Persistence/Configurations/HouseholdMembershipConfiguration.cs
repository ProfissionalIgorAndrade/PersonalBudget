using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HouseholdMembershipConfiguration : IEntityTypeConfiguration<HouseholdMembership>
{
    public void Configure(EntityTypeBuilder<HouseholdMembership> builder)
    {
        builder.ToTable("household_memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.HouseholdId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(m => m.JoinedAt).IsRequired();

        builder.HasIndex(m => new { m.HouseholdId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
