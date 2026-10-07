using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Cria a tabela <c>simulations</c>: simulações "E se...?" salvas no servidor, por lar e
    /// por dono. Sem chaves estrangeiras, como no resto do modelo.
    ///
    /// Tabela nova e vazia: não altera nem move dados existentes. O <c>Down</c> apaga a tabela
    /// e, com ela, todas as simulações salvas.
    /// </summary>
    public partial class AddSimulations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "simulations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    start_month = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_kind = table.Column<int>(type: "integer", nullable: false),
                    installments = table.Column<int>(type: "integer", nullable: true),
                    months = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_simulations_household_id",
                table: "simulations",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulations_owner_user_id",
                table: "simulations",
                column: "owner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "simulations");
        }
    }
}
