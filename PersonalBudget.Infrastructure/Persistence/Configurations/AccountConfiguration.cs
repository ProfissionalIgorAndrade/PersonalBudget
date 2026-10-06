using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.HouseholdId)
            .IsRequired();

        builder.Property(a => a.MemberProfileId)
            .HasColumnName("member_profile_id");

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.HouseholdId);
        builder.HasIndex(a => a.MemberProfileId);

        // 🔹 Bank enum
        builder.Property(a => a.Bank)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.IsActive)
            .IsRequired();

        builder.Property(a => a.Kind)
            .HasColumnName("kind")
            .HasConversion<int>()
            .HasDefaultValue(AccountKind.Checking);

        builder.Property(a => a.ParentAccountId)
            .HasColumnName("parent_account_id");

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(Account.NameMaxLength);

        builder.Property(a => a.SavingsGoal)
            .HasColumnName("savings_goal")
            .HasColumnType("numeric");

        // Caixinhas de uma conta são buscadas juntas o tempo todo.
        builder.HasIndex(a => a.ParentAccountId);
    }
}
