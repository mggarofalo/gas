using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOctaneRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "octane_rating",
                table: "vehicles",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "octane_rating",
                table: "fill_ups",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "octane_rating",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "octane_rating",
                table: "fill_ups");
        }
    }
}
