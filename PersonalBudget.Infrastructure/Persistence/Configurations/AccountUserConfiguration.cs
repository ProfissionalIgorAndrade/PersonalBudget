using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AccountUserConfiguration : IEntityTypeConfiguration<AccountUser>
{
    public void Configure(EntityTypeBuilder<AccountUser> builder)
    {
        builder.ToTable("account_users");

        builder.HasKey(au => au.Id);

        builder.Property(au => au.Role)
            .IsRequired();

        builder.Property(au => au.JoinedAt)
            .IsRequired();

        builder.HasIndex(au => new { au.AccountId, au.UserId })
            .IsUnique();
    }
}
