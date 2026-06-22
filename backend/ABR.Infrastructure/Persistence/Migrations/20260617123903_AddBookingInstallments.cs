using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_installments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    milestone_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    due_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    paid_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_installments", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_installments_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_installments_condition_items_condition_item_id",
                        column: x => x.condition_item_id,
                        principalTable: "condition_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_installments_booking_id",
                table: "booking_installments",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_installments_condition_item_id",
                table: "booking_installments",
                column: "condition_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_installments");
        }
    }
}
