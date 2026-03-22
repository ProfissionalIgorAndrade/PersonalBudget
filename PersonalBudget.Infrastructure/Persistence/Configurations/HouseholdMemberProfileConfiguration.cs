using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HouseholdMemberProfileConfiguration : IEntityTypeConfiguration<HouseholdMemberProfile>
{
    public void Configure(EntityTypeBuilder<HouseholdMemberProfile> builder)
    {
        builder.ToTable("household_member_profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.HouseholdId).IsRequired();
        builder.Property(p => p.Kind)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(120);
        builder.Property(p => p.SortOrder).IsRequired();

        builder.HasIndex(p => p.HouseholdId);
    }
}
