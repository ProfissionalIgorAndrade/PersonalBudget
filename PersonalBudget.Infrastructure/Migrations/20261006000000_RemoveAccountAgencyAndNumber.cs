using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Remove as colunas <c>agency_number</c> e <c>account_number</c> da tabela
    /// <c>accounts</c>.
    ///
    /// A conta deixou de ter agência e número: nada na lógica dependia deles e eram
    /// obrigatórios só por herança do modelo inicial. Os dados são descartados.
    /// O <c>Down</c> recria as colunas vazias (esquema, não os valores).
    /// </summary>
    public partial class RemoveAccountAgencyAndNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agency_number",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "account_number",
                table: "accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agency_number",
                table: "accounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "account_number",
                table: "accounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
