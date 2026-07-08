using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalVoucherModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "journal_vouchers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    voucher_date = table.Column<DateOnly>(type: "date", nullable: false),
                    narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    total_debit = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    total_credit = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_vouchers", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_vouchers_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "journal_voucher_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    journal_voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_voucher_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_voucher_lines_journal_vouchers_journal_voucher_id",
                        column: x => x.journal_voucher_id,
                        principalTable: "journal_vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_journal_voucher_lines_sub_ledgers_sub_ledger_id",
                        column: x => x.sub_ledger_id,
                        principalTable: "sub_ledgers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_journal_voucher_lines_journal_voucher_id_line_no",
                table: "journal_voucher_lines",
                columns: new[] { "journal_voucher_id", "line_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_voucher_lines_sub_ledger_id",
                table: "journal_voucher_lines",
                column: "sub_ledger_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_vouchers_site_id_voucher_date_is_deleted",
                table: "journal_vouchers",
                columns: new[] { "site_id", "voucher_date", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_journal_vouchers_site_id_voucher_no",
                table: "journal_vouchers",
                columns: new[] { "site_id", "voucher_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "journal_voucher_lines");

            migrationBuilder.DropTable(
                name: "journal_vouchers");
        }
    }
}
