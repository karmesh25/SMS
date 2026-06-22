using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVyajKhataTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vyaj_parties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vyaj_parties", x => x.id);
                    table.ForeignKey(
                        name: "FK_vyaj_parties_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vyaj_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    principal = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    rate_percent = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    rate_basis = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vyaj_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_vyaj_entries_vyaj_parties_party_id",
                        column: x => x.party_id,
                        principalTable: "vyaj_parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vyaj_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    payment_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vyaj_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_vyaj_payments_vyaj_entries_entry_id",
                        column: x => x.entry_id,
                        principalTable: "vyaj_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vyaj_entries_party_id_is_deleted",
                table: "vyaj_entries",
                columns: new[] { "party_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_vyaj_parties_site_id_is_deleted",
                table: "vyaj_parties",
                columns: new[] { "site_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_vyaj_payments_entry_id_is_deleted",
                table: "vyaj_payments",
                columns: new[] { "entry_id", "is_deleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vyaj_payments");

            migrationBuilder.DropTable(
                name: "vyaj_entries");

            migrationBuilder.DropTable(
                name: "vyaj_parties");
        }
    }
}
