using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Remove o vínculo cartão-conta, o dia de fechamento e o status da fatura.
    ///
    /// O cartão deixa de ter conta de débito (<c>credit_cards.AccountId</c>) e dia de
    /// fechamento (<c>ClosingDay</c>); a compra de cartão passa a ter
    /// <c>transactions.AccountId</c> nulo, então deixa de reduzir o saldo de qualquer conta.
    /// A fatura deixa de guardar <c>Status</c>, período, fechamento, vencimento,
    /// <c>PaidFromAccountId</c> e <c>RefundTransactionId</c>, e passa a ser identificada
    /// por (cartão, <c>StatementYear</c>, <c>StatementMonth</c>) com índice único. O vencimento
    /// é calculado a partir do <c>DueDay</c> do cartão.
    ///
    /// Operações sobre dados reais:
    /// o <c>AccountId</c> de todas as compras de cartão é zerado, e faturas duplicadas por
    /// (cartão, ano, mês) são mescladas antes de criar o índice único: as transações vão para
    /// a fatura de menor <c>Id</c>, o total da mantida passa a ser a soma do grupo e as
    /// duplicadas são apagadas. Os valores de <c>ClosingMonth</c>/<c>ClosingYear</c> são
    /// preservados, apenas renomeados.
    ///
    /// O <c>Down</c> recria só o esquema, não os dados: colunas removidas voltam com valores
    /// padrão (<c>Status</c> = Open, datas = 0001-01-01, <c>ClosingDay</c> = 0, <c>AccountId</c>
    /// do cartão e das compras de cartão = Guid vazio), faturas mescladas não se separam e o
    /// vínculo com a conta de débito não é recuperável.
    /// </summary>
    public partial class RemoveCardClosingDebitAndStatementStatus : Migration
    {
        // Ranking das faturas por (cartão, ano, mês): rn = 1 é a mantida (menor Id).
        // Postgres não tem min(uuid), então o menor Id sai de uma window function ordenada
        // pelo texto do uuid. Aspas duplas dentro do @"" precisam ser dobradas.
        private const string RankedStatements = @"
                SELECT ""Id"",
                       row_number() OVER (
                           PARTITION BY ""CreditCardId"", ""ClosingYear"", ""ClosingMonth""
                           ORDER BY ""Id""::text) AS rn,
                       first_value(""Id"") OVER (
                           PARTITION BY ""CreditCardId"", ""ClosingYear"", ""ClosingMonth""
                           ORDER BY ""Id""::text) AS keep_id,
                       count(*) OVER (
                           PARTITION BY ""CreditCardId"", ""ClosingYear"", ""ClosingMonth"") AS group_size,
                       sum(total_amount) OVER (
                           PARTITION BY ""CreditCardId"", ""ClosingYear"", ""ClosingMonth"") AS group_total
                FROM credit_card_statements";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Compra de cartão não terá conta.
            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                table: "transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // 2. PaymentMethod 2 = CreditCard (coluna int). Idempotente.
            migrationBuilder.Sql(@"UPDATE transactions SET ""AccountId"" = NULL WHERE ""PaymentMethod"" = 2;");

            // 3. Cartão sem conta de débito e sem dia de fechamento.
            migrationBuilder.DropIndex(
                name: "IX_credit_cards_AccountId",
                table: "credit_cards");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "credit_cards");

            migrationBuilder.DropColumn(
                name: "ClosingDay",
                table: "credit_cards");

            // 4. Deduplica faturas por (CreditCardId, ClosingYear, ClosingMonth) antes do
            // índice único. Só mexe em grupos com mais de uma fatura, então rodar de novo
            // não altera nada.
            // 4a. A fatura mantida recebe a soma dos totais do grupo.
            migrationBuilder.Sql($@"
                WITH ranked AS ({RankedStatements})
                UPDATE credit_card_statements s
                SET total_amount = r.group_total
                FROM ranked r
                WHERE s.""Id"" = r.""Id""
                  AND r.rn = 1
                  AND r.group_size > 1;");

            // 4b. As transações das duplicadas passam para a fatura mantida.
            migrationBuilder.Sql($@"
                WITH ranked AS ({RankedStatements})
                UPDATE transactions t
                SET ""StatementId"" = r.keep_id
                FROM ranked r
                WHERE t.""StatementId"" = r.""Id""
                  AND r.rn > 1;");

            // 4c. Apaga as duplicadas, já sem transações.
            migrationBuilder.Sql($@"
                WITH ranked AS ({RankedStatements})
                DELETE FROM credit_card_statements s
                USING ranked r
                WHERE s.""Id"" = r.""Id""
                  AND r.rn > 1;");

            // 5. Fatura sem status, período, fechamento, vencimento e colunas de pagamento.
            migrationBuilder.DropIndex(
                name: "IX_credit_card_statements_CreditCardId_PeriodStart_PeriodEnd",
                table: "credit_card_statements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "credit_card_statements");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "credit_card_statements");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "credit_card_statements");

            migrationBuilder.DropColumn(
                name: "ClosingDate",
                table: "credit_card_statements");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "credit_card_statements");

            migrationBuilder.DropColumn(
                name: "PaidFromAccountId",
                table: "credit_card_statements");

            migrationBuilder.DropColumn(
                name: "RefundTransactionId",
                table: "credit_card_statements");

            // 6. Mês e ano da fatura (os valores guardados são mantidos) e identidade única.
            migrationBuilder.RenameColumn(
                name: "ClosingMonth",
                table: "credit_card_statements",
                newName: "StatementMonth");

            migrationBuilder.RenameColumn(
                name: "ClosingYear",
                table: "credit_card_statements",
                newName: "StatementYear");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_statements_CreditCardId_StatementYear_StatementMonth",
                table: "credit_card_statements",
                columns: new[] { "CreditCardId", "StatementYear", "StatementMonth" },
                unique: true);

            // 7. Toda visão de fatura filtra transações por StatementId.
            migrationBuilder.CreateIndex(
                name: "IX_transactions_StatementId",
                table: "transactions",
                column: "StatementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_StatementId",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_credit_card_statements_CreditCardId_StatementYear_StatementMonth",
                table: "credit_card_statements");

            migrationBuilder.RenameColumn(
                name: "StatementMonth",
                table: "credit_card_statements",
                newName: "ClosingMonth");

            migrationBuilder.RenameColumn(
                name: "StatementYear",
                table: "credit_card_statements",
                newName: "ClosingYear");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "credit_card_statements",
                type: "text",
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodStart",
                table: "credit_card_statements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodEnd",
                table: "credit_card_statements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosingDate",
                table: "credit_card_statements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "credit_card_statements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<Guid>(
                name: "PaidFromAccountId",
                table: "credit_card_statements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefundTransactionId",
                table: "credit_card_statements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_statements_CreditCardId_PeriodStart_PeriodEnd",
                table: "credit_card_statements",
                columns: new[] { "CreditCardId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "credit_cards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ClosingDay",
                table: "credit_cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_credit_cards_AccountId",
                table: "credit_cards",
                column: "AccountId");

            // O esquema antigo exige conta em toda transação. O vínculo original das
            // compras de cartão não é recuperável, então elas recebem o Guid vazio.
            migrationBuilder.Sql(@"UPDATE transactions SET ""AccountId"" = '00000000-0000-0000-0000-000000000000' WHERE ""AccountId"" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                table: "transactions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
