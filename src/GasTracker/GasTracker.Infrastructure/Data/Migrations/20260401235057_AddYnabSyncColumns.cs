using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddYnabSyncColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ynab_sync_error",
                table: "fill_ups",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ynab_sync_status",
                table: "fill_ups",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.AddColumn<string>(
                name: "ynab_transaction_id",
                table: "fill_ups",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ynab_sync_error",
                table: "fill_ups");

            migrationBuilder.DropColumn(
                name: "ynab_sync_status",
                table: "fill_ups");

            migrationBuilder.DropColumn(
                name: "ynab_transaction_id",
                table: "fill_ups");
        }
    }
}
