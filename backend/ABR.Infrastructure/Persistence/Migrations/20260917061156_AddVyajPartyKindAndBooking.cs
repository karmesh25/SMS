using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVyajPartyKindAndBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "booking_id",
                table: "vyaj_parties",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "party_kind",
                table: "vyaj_parties",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "direct");

            migrationBuilder.CreateIndex(
                name: "IX_vyaj_parties_booking_id",
                table: "vyaj_parties",
                column: "booking_id");

            migrationBuilder.AddForeignKey(
                name: "FK_vyaj_parties_bookings_booking_id",
                table: "vyaj_parties",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vyaj_parties_bookings_booking_id",
                table: "vyaj_parties");

            migrationBuilder.DropIndex(
                name: "IX_vyaj_parties_booking_id",
                table: "vyaj_parties");

            migrationBuilder.DropColumn(
                name: "booking_id",
                table: "vyaj_parties");

            migrationBuilder.DropColumn(
                name: "party_kind",
                table: "vyaj_parties");
        }
    }
}
