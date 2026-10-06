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

        builder.Property(x => x.StatementMonth)
            .IsRequired();

        builder.Property(x => x.StatementYear)
            .IsRequired();

        builder.OwnsOne(a => a.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .IsRequired();
        });

        builder.HasOne<CreditCard>()
            .WithMany()
            .HasForeignKey(x => x.CreditCardId)
            .OnDelete(DeleteBehavior.Cascade);

        // Uma fatura por cartão, mês e ano. Também atende as buscas por cartão.
        builder.HasIndex(x => new { x.CreditCardId, x.StatementYear, x.StatementMonth })
            .IsUnique();
    }
}
