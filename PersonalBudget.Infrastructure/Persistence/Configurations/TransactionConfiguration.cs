using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.HouseholdId)
            .IsRequired();

        builder.Property(t => t.AttributionProfileId)
            .IsRequired();

        builder.Property(t => t.AccountId)
            .IsRequired();

        builder.Property(t => t.CategoryId);

        builder.Property(t => t.CreditCardId);

        builder.Property(t => t.StatementId);

        builder.Property(t => t.TransferId);

        builder.Property(t => t.RecurrenceId)
            .HasColumnName("recurrence_id");

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.PaymentMethod)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.Frequency)
            .IsRequired()
            .HasColumnName("frequency")
            .HasConversion<int>();

        builder.Property(t => t.ExpirationDate)
            .HasColumnName("expiration_date");

        builder.Property(t => t.DueDate)
            .HasColumnName("due_date");

        // 🔹 Money (Value Object)
        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .IsRequired();
        });

        // 🔹 TransactionDate (Value Object)
        builder.OwnsOne(t => t.Date, date =>
        {
            date.Property(d => d.Value)
                .HasColumnName("transaction_date")
                .IsRequired();
        });

        // 🔹 Description (Value Object)
        builder.OwnsOne(t => t.Description, desc =>
        {
            desc.Property(d => d.Value)
                .HasColumnName("description")
                .HasMaxLength(200)
                .IsRequired();
        });

        builder.Property(t => t.Observations)
            .HasColumnName("observations")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.HouseholdId);
        builder.HasIndex(t => t.AttributionProfileId);
        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => t.CreditCardId);
        builder.HasIndex(t => t.TransferId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.RecurrenceId);
    }
}
