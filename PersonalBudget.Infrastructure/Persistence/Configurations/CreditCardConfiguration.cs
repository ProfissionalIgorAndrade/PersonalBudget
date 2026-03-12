
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("credit_cards");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.AccountId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Limit)
            .IsRequired();

        builder.Property(c => c.ClosingDay)
            .IsRequired();

        builder.Property(c => c.DueDay)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.HasIndex(c => c.UserId);
        
        builder.HasIndex(c => c.AccountId);

        builder.HasMany(c => c.Statements)
            .WithOne()
            .HasForeignKey(x => x.CreditCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
