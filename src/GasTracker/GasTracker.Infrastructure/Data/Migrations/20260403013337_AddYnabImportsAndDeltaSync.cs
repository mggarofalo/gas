using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddYnabImportsAndDeltaSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "last_server_knowledge",
                table: "ynab_settings",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ynab_imports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ynab_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    payee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount_milliunits = table.Column<long>(type: "bigint", nullable: false),
                    memo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    gallons = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    price_per_gallon = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    octane_rating = table.Column<short>(type: "smallint", nullable: true),
                    odometer_miles = table.Column<int>(type: "integer", nullable: true),
                    vehicle_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ynab_imports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ynab_imports_status",
                table: "ynab_imports",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_ynab_imports_ynab_transaction_id",
                table: "ynab_imports",
                column: "ynab_transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ynab_imports");

            migrationBuilder.DropColumn(
                name: "last_server_knowledge",
                table: "ynab_settings");
        }
    }
}
