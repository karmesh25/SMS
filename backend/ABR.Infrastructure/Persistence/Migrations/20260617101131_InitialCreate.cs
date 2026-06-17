using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sites", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    force_password_change = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ifsc_code = table.Column<string>(type: "text", nullable: true),
                    branch = table.Column<string>(type: "text", nullable: true),
                    opening_balance = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_bank_accounts_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "brokers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_no = table.Column<string>(type: "text", nullable: true),
                    contact_no_2 = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brokers", x => x.id);
                    table.ForeignKey(
                        name: "FK_brokers_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conditions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    condition_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conditions", x => x.id);
                    table.ForeignKey(
                        name: "FK_conditions_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "main_ledgers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ledger_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_main_ledgers", x => x.id);
                    table.ForeignKey(
                        name: "FK_main_ledgers_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wing_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    floors = table.Column<int>(type: "integer", nullable: false),
                    flats_per_floor = table.Column<int>(type: "integer", nullable: false),
                    shops = table.Column<int>(type: "integer", nullable: false),
                    is_bungalow = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wings", x => x.id);
                    table.ForeignKey(
                        name: "FK_wings_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    table_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "device_licenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fingerprint_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    authorized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_licenses", x => x.id);
                    table.ForeignKey(
                        name: "FK_device_licenses_users_authorized_by",
                        column: x => x.authorized_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_site_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    can_read = table.Column<bool>(type: "boolean", nullable: false),
                    can_write = table.Column<bool>(type: "boolean", nullable: false),
                    can_delete = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_site_access", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_site_access_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_site_access_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "condition_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    condition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    milestone_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    due_after_days = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_condition_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_condition_items_conditions_condition_id",
                        column: x => x.condition_id,
                        principalTable: "conditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "flats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    wing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sqft = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    flat_type = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flats", x => x.id);
                    table.ForeignKey(
                        name: "FK_flats_wings_wing_id",
                        column: x => x.wing_id,
                        principalTable: "wings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sub_ledgers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    main_ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ledger_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sub_ledgers", x => x.id);
                    table.ForeignKey(
                        name: "FK_sub_ledgers_flats_flat_id",
                        column: x => x.flat_id,
                        principalTable: "flats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sub_ledgers_main_ledgers_main_ledger_id",
                        column: x => x.main_ledger_id,
                        principalTable: "main_ledgers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_sub_ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    broker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    condition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_contact = table.Column<string>(type: "text", nullable: true),
                    sqft = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    brokerage_pct = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    brokerage_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    customer_type = table.Column<string>(type: "text", nullable: false),
                    is_arja_marja_sell = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    cancel_date = table.Column<DateOnly>(type: "date", nullable: true),
                    dastavej_date = table.Column<DateOnly>(type: "date", nullable: true),
                    satakhat_date = table.Column<DateOnly>(type: "date", nullable: true),
                    document_number = table.Column<string>(type: "text", nullable: true),
                    service_tax = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_bookings_brokers_broker_id",
                        column: x => x.broker_id,
                        principalTable: "brokers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_bookings_conditions_condition_id",
                        column: x => x.condition_id,
                        principalTable: "conditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bookings_flats_flat_id",
                        column: x => x.flat_id,
                        principalTable: "flats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bookings_sub_ledgers_member_sub_ledger_id",
                        column: x => x.member_sub_ledger_id,
                        principalTable: "sub_ledgers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    main_ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    cash_bank = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_daily_entries_main_ledgers_main_ledger_id",
                        column: x => x.main_ledger_id,
                        principalTable: "main_ledgers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_entries_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_entries_sub_ledgers_sub_ledger_id",
                        column: x => x.sub_ledger_id,
                        principalTable: "sub_ledgers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_accounts_site_id",
                table: "bank_accounts",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_broker_id",
                table: "bookings",
                column: "broker_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_condition_id",
                table: "bookings",
                column: "condition_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_flat_id",
                table: "bookings",
                column: "flat_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_member_sub_ledger_id",
                table: "bookings",
                column: "member_sub_ledger_id");

            migrationBuilder.CreateIndex(
                name: "IX_brokers_site_id",
                table: "brokers",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_condition_items_condition_id",
                table: "condition_items",
                column: "condition_id");

            migrationBuilder.CreateIndex(
                name: "IX_conditions_site_id",
                table: "conditions",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_entries_main_ledger_id",
                table: "daily_entries",
                column: "main_ledger_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_entries_site_id_entry_date_entry_type_is_deleted",
                table: "daily_entries",
                columns: new[] { "site_id", "entry_date", "entry_type", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_entries_sub_ledger_id",
                table: "daily_entries",
                column: "sub_ledger_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_licenses_authorized_by",
                table: "device_licenses",
                column: "authorized_by");

            migrationBuilder.CreateIndex(
                name: "IX_flats_wing_id_flat_no",
                table: "flats",
                columns: new[] { "wing_id", "flat_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_main_ledgers_site_id",
                table: "main_ledgers",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_sub_ledgers_flat_id",
                table: "sub_ledgers",
                column: "flat_id");

            migrationBuilder.CreateIndex(
                name: "IX_sub_ledgers_main_ledger_id",
                table: "sub_ledgers",
                column: "main_ledger_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_site_access_site_id",
                table: "user_site_access",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_site_access_user_id",
                table: "user_site_access",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wings_site_id",
                table: "wings",
                column: "site_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "bank_accounts");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "condition_items");

            migrationBuilder.DropTable(
                name: "daily_entries");

            migrationBuilder.DropTable(
                name: "device_licenses");

            migrationBuilder.DropTable(
                name: "user_site_access");

            migrationBuilder.DropTable(
                name: "brokers");

            migrationBuilder.DropTable(
                name: "conditions");

            migrationBuilder.DropTable(
                name: "sub_ledgers");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "flats");

            migrationBuilder.DropTable(
                name: "main_ledgers");

            migrationBuilder.DropTable(
                name: "wings");

            migrationBuilder.DropTable(
                name: "sites");
        }
    }
}
