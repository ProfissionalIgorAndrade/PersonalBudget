using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Caixinhas: contas de poupança vinculadas a uma conta corrente.
    ///
    /// Modeladas como uma variação de conta em vez de entidade nova, porque
    /// guardar dinheiro é mover saldo entre duas contas — que é exatamente o
    /// que a transferência já faz, e transferência já fica fora dos totais de
    /// receita e despesa.
    ///
    /// kind tem default 1 (Checking), então toda conta existente continua
    /// sendo conta corrente sem precisar de backfill.
    /// </summary>
    public partial class AddSavingsBoxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "kind",
                table: "accounts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_account_id",
                table: "accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "accounts",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_parent_account_id",
                table: "accounts",
                column: "parent_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_accounts_parent_account_id", table: "accounts");
            migrationBuilder.DropColumn(name: "name", table: "accounts");
            migrationBuilder.DropColumn(name: "parent_account_id", table: "accounts");
            migrationBuilder.DropColumn(name: "kind", table: "accounts");
        }
    }
}
