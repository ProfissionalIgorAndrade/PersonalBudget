using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Índice composto (household_id, transaction_date DESC) na tabela transactions.
    ///
    /// Necessário para que as queries de date-range do Calendário Financeiro
    /// não façam full-table-scan à medida que o volume de lançamentos cresce.
    /// </summary>
    public partial class AddTransactionDateHouseholdIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_transactions_household_date
                ON transactions (""HouseholdId"", transaction_date DESC NULLS LAST);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ix_transactions_household_date;
            ");
        }
    }
}
