using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueYnabTransactionIdOnFillUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_fill_ups_ynab_transaction_id",
                table: "fill_ups",
                column: "ynab_transaction_id",
                unique: true,
                filter: "ynab_transaction_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_fill_ups_ynab_transaction_id",
                table: "fill_ups");
        }
    }
}
