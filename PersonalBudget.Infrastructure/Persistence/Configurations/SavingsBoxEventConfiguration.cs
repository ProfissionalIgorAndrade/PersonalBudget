using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SavingsBoxEventConfiguration : IEntityTypeConfiguration<SavingsBoxEvent>
{
    public void Configure(EntityTypeBuilder<SavingsBoxEvent> builder)
    {
        builder.ToTable("savings_box_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.HouseholdId)
            .HasColumnName("household_id")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(e => e.BoxName)
            .HasColumnName("box_name")
            .IsRequired()
            .HasMaxLength(SavingsBoxEvent.NameMaxLength);

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Reason)
            .HasColumnName("reason")
            .HasMaxLength(SavingsBoxEvent.ReasonMaxLength);

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .IsRequired();

        builder.Property(e => e.DestinationAccountId)
            .HasColumnName("destination_account_id");

        builder.Property(e => e.DestinationName)
            .HasColumnName("destination_name")
            .HasMaxLength(SavingsBoxEvent.NameMaxLength);

        builder.Property(e => e.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.HasIndex(e => e.HouseholdId);
    }
}
