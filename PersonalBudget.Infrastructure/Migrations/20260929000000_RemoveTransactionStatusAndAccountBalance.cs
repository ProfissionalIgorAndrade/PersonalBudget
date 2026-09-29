using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Remove o campo <c>status</c> da tabela <c>transactions</c> e o campo
    /// <c>balance</c> da tabela <c>accounts</c>.
    ///
    /// Status deixou de existir — todas as transações são válidas ao serem criadas.
    /// Saldo passou a ser calculado dinamicamente como Σ receitas − Σ despesas.
    /// </summary>
    public partial class RemoveTransactionStatusAndAccountBalance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transactions_status",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "balance",
                table: "accounts");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "transactions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_status",
                table: "transactions",
                column: "status");

            migrationBuilder.AddColumn<decimal>(
                name: "balance",
                table: "accounts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
