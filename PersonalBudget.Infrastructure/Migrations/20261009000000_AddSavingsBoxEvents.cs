using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Cria a tabela <c>savings_box_events</c>: histórico de criação e exclusão de caixinhas,
    /// por lar. Sem chaves estrangeiras, como no resto do modelo.
    ///
    /// Tabela nova e vazia: não altera nem move dados existentes. O <c>Down</c> apaga a tabela
    /// e, com ela, todo o histórico de caixinhas.
    /// </summary>
    public partial class AddSavingsBoxEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "savings_box_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    box_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    destination_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_savings_box_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_savings_box_events_household_id",
                table: "savings_box_events",
                column: "household_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "savings_box_events");
        }
    }
}
