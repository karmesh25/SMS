using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ABR.Infrastructure.Persistence;

#nullable disable

namespace ABR.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AbrDbContext))]
[Migration("20260918150000_AddNotificationResolvedFields")]
public partial class AddNotificationResolvedFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "resolved_at",
            table: "user_notifications",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "resolved_by_user_id",
            table: "user_notifications",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_notifications_resolved_by_user_id",
            table: "user_notifications",
            column: "resolved_by_user_id");

        migrationBuilder.CreateIndex(
            name: "IX_user_notifications_entity_id_entity_type_resolved_at",
            table: "user_notifications",
            columns: new[] { "entity_id", "entity_type", "resolved_at" });

        migrationBuilder.AddForeignKey(
            name: "FK_user_notifications_users_resolved_by_user_id",
            table: "user_notifications",
            column: "resolved_by_user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_user_notifications_users_resolved_by_user_id",
            table: "user_notifications");

        migrationBuilder.DropIndex(
            name: "IX_user_notifications_resolved_by_user_id",
            table: "user_notifications");

        migrationBuilder.DropIndex(
            name: "IX_user_notifications_entity_id_entity_type_resolved_at",
            table: "user_notifications");

        migrationBuilder.DropColumn(
            name: "resolved_at",
            table: "user_notifications");

        migrationBuilder.DropColumn(
            name: "resolved_by_user_id",
            table: "user_notifications");
    }
}
