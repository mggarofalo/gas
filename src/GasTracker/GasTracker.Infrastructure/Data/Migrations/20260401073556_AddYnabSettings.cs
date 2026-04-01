using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddYnabSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ynab_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    api_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    plan_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    plan_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    account_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ynab_settings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ynab_settings");
        }
    }
}
