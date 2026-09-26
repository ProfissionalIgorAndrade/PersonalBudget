using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Meta por caixinha.
    ///
    /// Nullable porque nem toda caixinha tem meta, e zero significaria meta
    /// batida em vez de meta ausente.
    /// </summary>
    public partial class AddSavingsGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "savings_goal",
                table: "accounts",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "savings_goal", table: "accounts");
        }
    }
}
