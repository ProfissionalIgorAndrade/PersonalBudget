using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(a => a.Balance, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("balance")
                 .IsRequired();
        });

        builder.Property(a => a.CreatedAt)
            .IsRequired();
    }
}
