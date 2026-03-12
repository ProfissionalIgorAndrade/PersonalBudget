using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CreditCardStatementConfiguration : IEntityTypeConfiguration<CreditCardStatement>
{
    public void Configure(EntityTypeBuilder<CreditCardStatement> builder)
    {
        builder.ToTable("credit_card_statements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreditCardId)
            .IsRequired();

        builder.Property(x => x.PeriodStart)
            .IsRequired();

        builder.Property(x => x.PeriodEnd)
            .IsRequired();

        builder.Property(x => x.ClosingDate)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .IsRequired();

        builder.Property(x => x.ClosingMonth)
            .IsRequired();
            
        builder.Property(x => x.ClosingYear)
            .IsRequired();

        builder.OwnsOne(a => a.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .IsRequired();
        });

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(x => new { x.CreditCardId, x.PeriodStart, x.PeriodEnd });
    }
}