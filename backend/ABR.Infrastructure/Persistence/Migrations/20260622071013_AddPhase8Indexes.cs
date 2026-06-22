using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase8Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_flats_wing_id_status",
                table: "flats",
                columns: new[] { "wing_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_device_licenses_fingerprint_hash",
                table: "device_licenses",
                column: "fingerprint_hash");

            migrationBuilder.CreateIndex(
                name: "IX_daily_entries_site_id_main_ledger_id_sub_ledger_id",
                table: "daily_entries",
                columns: new[] { "site_id", "main_ledger_id", "sub_ledger_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_flats_wing_id_status",
                table: "flats");

            migrationBuilder.DropIndex(
                name: "IX_device_licenses_fingerprint_hash",
                table: "device_licenses");

            migrationBuilder.DropIndex(
                name: "IX_daily_entries_site_id_main_ledger_id_sub_ledger_id",
                table: "daily_entries");
        }
    }
}
