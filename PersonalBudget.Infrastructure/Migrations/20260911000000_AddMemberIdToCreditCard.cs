using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBudget.Infrastructure.Migrations
{
    /// <summary>
    /// Vincula o cartão a um perfil de membro do lar.
    ///
    /// Até aqui o cartão só tinha UserId, que identifica quem o criou. A
    /// interface oferecia um campo "Membro" que era enviado e descartado
    /// silenciosamente, e nenhum cartão jamais exibiu o dono correto.
    ///
    /// Nullable e sem valor padrão: os cartões existentes ficam sem membro até
    /// serem editados. Preencher automaticamente a partir de UserId poria o
    /// nome de quem criou, que é exatamente o erro que isto corrige.
    /// </summary>
    public partial class AddMemberIdToCreditCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "member_id",
                table: "credit_cards",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "member_id",
                table: "credit_cards");
        }
    }
}
