using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Devolve todas as faturas ao estado aberto.
    ///
    /// Com a mudança de status removida, uma fatura gravada como Closed ou
    /// Paid ficaria presa: as regras de domínio recusam lançar nela e não há
    /// mais nenhum caminho que a reabra. Sem este passo, faturas antigas
    /// viravam somente-leitura permanentemente.
    ///
    /// A coluna em si é mantida por ora - removê-la é uma mudança de schema
    /// maior, e misturá-la aqui aumentaria o risco de um PR que já remove
    /// bastante comportamento.
    /// </summary>
    public partial class ReopenAllStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A coluna é texto, não inteiro: CreditCardStatementConfiguration usa
            // HasConversion<string>(), então o enum é gravado pelo nome. Comparar
            // com 1 produz "operator does not exist: text <> integer" e derruba o
            // MigrateAsync inteiro no startup.
            migrationBuilder.Sql(@"UPDATE credit_card_statements SET ""Status"" = 'Open' WHERE ""Status"" <> 'Open';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sem volta: o estado anterior de cada fatura não é recuperável
            // depois de sobrescrito.
        }
    }
}
