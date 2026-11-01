using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCategoryCacheIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_ynab_category_cache_category_id",
                table: "ynab_category_cache",
                column: "category_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ynab_category_cache_category_id",
                table: "ynab_category_cache");
        }
    }
}
