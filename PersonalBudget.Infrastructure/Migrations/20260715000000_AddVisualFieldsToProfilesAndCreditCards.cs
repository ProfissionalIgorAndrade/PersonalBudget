using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisualFieldsToProfilesAndCreditCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "household_member_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Emoji",
                table: "household_member_profiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "credit_cards",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "household_member_profiles");

            migrationBuilder.DropColumn(
                name: "Emoji",
                table: "household_member_profiles");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "credit_cards");
        }
    }
}
