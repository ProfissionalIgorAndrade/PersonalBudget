using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HouseholdInviteConfiguration : IEntityTypeConfiguration<HouseholdInvite>
{
    public void Configure(EntityTypeBuilder<HouseholdInvite> builder)
    {
        builder.ToTable("household_invites");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.HouseholdId).IsRequired();
        builder.Property(i => i.InviterUserId).IsRequired();
        builder.Property(i => i.InviteeEmailNormalized)
            .IsRequired()
            .HasMaxLength(320);
        builder.Property(i => i.Token)
            .IsRequired()
            .HasMaxLength(64);
        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(i => i.ExpiresAt).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasIndex(i => new { i.HouseholdId, i.InviteeEmailNormalized, i.Status });
    }
}
