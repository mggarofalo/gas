using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerFillUpCategoryAndCategoryCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ynab_category_id",
                table: "fill_ups",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ynab_category_name",
                table: "fill_ups",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ynab_category_cache",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category_group_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ynab_category_cache", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ynab_category_cache");

            migrationBuilder.DropColumn(
                name: "ynab_category_id",
                table: "fill_ups");

            migrationBuilder.DropColumn(
                name: "ynab_category_name",
                table: "fill_ups");
        }
    }
}
