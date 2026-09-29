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
            // Caixinhas que acumularam saldo via accounts.balance mas nunca tiveram
            // transações na tabela (ex: member_profile_id null → RecordSavingsMovementAsync
            // retornava early sem gravar) recebem uma transação de Income para preservar
            // o saldo histórico antes de a coluna balance ser removida.
            // NOT EXISTS torna o INSERT idempotente.
            // Aspas duplas dentro do @"" precisam ser dobradas ("" = literal ").
            migrationBuilder.Sql(@"
                INSERT INTO transactions
                    (""Id"", ""UserId"", ""HouseholdId"", ""AttributionProfileId"", ""AccountId"",
                     amount, ""Type"", ""PaymentMethod"", frequency,
                     transaction_date, description, ""Status"")
                SELECT
                    gen_random_uuid(),
                    a.""UserId"",
                    a.""HouseholdId"",
                    COALESCE(
                        a.member_profile_id,
                        (SELECT p.""Id""
                         FROM household_member_profiles p
                         WHERE p.""HouseholdId"" = a.""HouseholdId""
                         ORDER BY p.""Id""
                         LIMIT 1)
                    ),
                    a.""Id"",
                    a.balance,
                    1,
                    5,
                    1,
                    CURRENT_DATE,
                    'Saldo migrado',
                    2
                FROM accounts a
                WHERE a.kind = 2
                  AND a.balance > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM transactions t WHERE t.""AccountId"" = a.""Id""
                  )
                  AND EXISTS (
                      SELECT 1 FROM household_member_profiles p
                      WHERE p.""HouseholdId"" = a.""HouseholdId""
                  )
            ");

            migrationBuilder.DropIndex(
                name: "IX_transactions_Status",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "balance",
                table: "accounts");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "transactions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_Status",
                table: "transactions",
                column: "Status");

            migrationBuilder.AddColumn<decimal>(
                name: "balance",
                table: "accounts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
