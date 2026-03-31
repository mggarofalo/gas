using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fill_ups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    odometer_miles = table.Column<int>(type: "integer", nullable: false),
                    gallons = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    price_per_gallon = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    station_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    station_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    receipt_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    paperless_sync_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "none"),
                    paperless_document_id = table.Column<int>(type: "integer", nullable: true),
                    paperless_sync_error = table.Column<string>(type: "text", nullable: true),
                    paperless_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paperless_sync_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fill_ups", x => x.id);
                    table.ForeignKey(
                        name: "fk_fill_ups_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fill_ups_date",
                table: "fill_ups",
                column: "date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_fill_ups_vehicle_date",
                table: "fill_ups",
                columns: new[] { "vehicle_id", "date" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fill_ups");

            migrationBuilder.DropTable(
                name: "vehicles");
        }
    }
}
