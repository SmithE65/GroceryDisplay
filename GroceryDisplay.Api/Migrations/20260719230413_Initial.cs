using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GroceryDisplay.Api.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "grocery");

            migrationBuilder.CreateTable(
                name: "person",
                schema: "grocery",
                columns: table => new
                {
                    person_id = table.Column<string>(type: "text", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_person", x => x.person_id);
                    table.CheckConstraint("ck_person_id_format", "person_id ~ '^[a-z][a-z0-9_-]{1,31}$'");
                });

            migrationBuilder.CreateTable(
                name: "receipt",
                schema: "grocery",
                columns: table => new
                {
                    receipt_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    person_id = table.Column<string>(type: "text", maxLength: 32, nullable: false),
                    amount_cents = table.Column<int>(type: "integer", nullable: false),
                    purchased_on = table.Column<DateOnly>(type: "date", nullable: false),
                    store_name = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    client_entry_id = table.Column<string>(type: "text", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValue: "api"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    voided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "text", nullable: true),
                    void_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt", x => x.receipt_id);
                    table.CheckConstraint("ck_receipt_amount_cents_positive", "amount_cents >= 0");
                    table.CheckConstraint("ck_receipt_void_consistency", "(\r\n    voided_at is null\r\n    and voided_by is null\r\n    and void_reason is null\r\n)\r\nor\r\n(\r\n    voided_at is not null\r\n    and voided_by is not null\r\n    and void_reason is not null\r\n)");
                    table.ForeignKey(
                        name: "fk_receipt_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "grocery",
                        principalTable: "person",
                        principalColumn: "person_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_receipt_active_year_lookup",
                schema: "grocery",
                table: "receipt",
                columns: new[] { "purchased_on", "person_id" },
                filter: "voided_at is null");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_client_entry_id",
                schema: "grocery",
                table: "receipt",
                column: "client_entry_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_receipt_person_purchased_on",
                schema: "grocery",
                table: "receipt",
                columns: new[] { "person_id", "purchased_on" });

            migrationBuilder.CreateIndex(
                name: "ix_receipt_purchased_on",
                schema: "grocery",
                table: "receipt",
                column: "purchased_on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receipt",
                schema: "grocery");

            migrationBuilder.DropTable(
                name: "person",
                schema: "grocery");
        }
    }
}
