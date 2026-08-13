using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJvNarrationAndVyajEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "main_ledger_id",
                table: "vyaj_parties",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "sub_ledger_id",
                table: "vyaj_parties",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "emi_amount",
                table: "vyaj_entries",
                type: "numeric(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rate_period_months",
                table: "vyaj_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credit_narration",
                table: "journal_vouchers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "debit_narration",
                table: "journal_vouchers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vyaj_parties_main_ledger_id",
                table: "vyaj_parties",
                column: "main_ledger_id");

            migrationBuilder.CreateIndex(
                name: "IX_vyaj_parties_sub_ledger_id",
                table: "vyaj_parties",
                column: "sub_ledger_id");

            migrationBuilder.AddForeignKey(
                name: "FK_vyaj_parties_main_ledgers_main_ledger_id",
                table: "vyaj_parties",
                column: "main_ledger_id",
                principalTable: "main_ledgers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_vyaj_parties_sub_ledgers_sub_ledger_id",
                table: "vyaj_parties",
                column: "sub_ledger_id",
                principalTable: "sub_ledgers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vyaj_parties_main_ledgers_main_ledger_id",
                table: "vyaj_parties");

            migrationBuilder.DropForeignKey(
                name: "FK_vyaj_parties_sub_ledgers_sub_ledger_id",
                table: "vyaj_parties");

            migrationBuilder.DropIndex(
                name: "IX_vyaj_parties_main_ledger_id",
                table: "vyaj_parties");

            migrationBuilder.DropIndex(
                name: "IX_vyaj_parties_sub_ledger_id",
                table: "vyaj_parties");

            migrationBuilder.DropColumn(
                name: "main_ledger_id",
                table: "vyaj_parties");

            migrationBuilder.DropColumn(
                name: "sub_ledger_id",
                table: "vyaj_parties");

            migrationBuilder.DropColumn(
                name: "emi_amount",
                table: "vyaj_entries");

            migrationBuilder.DropColumn(
                name: "rate_period_months",
                table: "vyaj_entries");

            migrationBuilder.DropColumn(
                name: "credit_narration",
                table: "journal_vouchers");

            migrationBuilder.DropColumn(
                name: "debit_narration",
                table: "journal_vouchers");
        }
    }
}
