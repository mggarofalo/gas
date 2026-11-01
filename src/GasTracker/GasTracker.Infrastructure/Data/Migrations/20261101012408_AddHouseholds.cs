using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholds : Migration
    {
        public const string DefaultHouseholdId = "6f1c2b8e-3d4a-4c5b-9e7f-0a1b2c3d4e5f";

        // Tables that gain a required household_id. fill_ups inherit scope through vehicles.
        private static readonly string[] HouseholdTables =
        [
            "AspNetUsers",
            "vehicles",
            "vehicle_memo_mappings",
            "ynab_settings",
            "ynab_imports",
            "ynab_account_cache",
            "ynab_category_cache",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "households",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_households", x => x.id);
                });

            // Backfill: an existing single-user database gets one "Default" household that owns
            // every pre-existing row. A fresh database gets none; HouseholdBootstrapper creates it.
            var dataExists = string.Join(" OR ", HouseholdTables.Select(t => $"EXISTS (SELECT 1 FROM \"{t}\")"));
            migrationBuilder.Sql($"""
                INSERT INTO households (id, name, created_at, updated_at)
                SELECT '{DefaultHouseholdId}', 'Default', now(), now()
                WHERE {dataExists};
                """);

            foreach (var table in HouseholdTables)
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "household_id",
                    table: table,
                    type: "uuid",
                    nullable: true);

                migrationBuilder.Sql($"UPDATE \"{table}\" SET household_id = '{DefaultHouseholdId}';");

                migrationBuilder.AlterColumn<Guid>(
                    name: "household_id",
                    table: table,
                    type: "uuid",
                    nullable: false,
                    oldClrType: typeof(Guid),
                    oldType: "uuid",
                    oldNullable: true);
            }

            // Every pre-existing user was a seeded admin, so they own the default household.
            migrationBuilder.AddColumn<string>(
                name: "household_role",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("UPDATE \"AspNetUsers\" SET household_role = 'Owner';");

            migrationBuilder.AlterColumn<string>(
                name: "household_role",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "ix_ynab_imports_ynab_transaction_id",
                table: "ynab_imports");

            migrationBuilder.DropIndex(
                name: "ix_ynab_category_cache_category_id",
                table: "ynab_category_cache");

            migrationBuilder.DropIndex(
                name: "ix_vehicle_memo_mappings_memo_name",
                table: "vehicle_memo_mappings");

            migrationBuilder.CreateIndex(
                name: "ix_ynab_settings_household_id",
                table: "ynab_settings",
                column: "household_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ynab_imports_household_id_ynab_transaction_id",
                table: "ynab_imports",
                columns: new[] { "household_id", "ynab_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ynab_category_cache_household_id_category_id",
                table: "ynab_category_cache",
                columns: new[] { "household_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ynab_account_cache_household_id",
                table: "ynab_account_cache",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_household_id",
                table: "vehicles",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_memo_mappings_household_id_memo_name",
                table: "vehicle_memo_mappings",
                columns: new[] { "household_id", "memo_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_household_id",
                table: "AspNetUsers",
                column: "household_id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_users_households_household_id",
                table: "AspNetUsers",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vehicle_memo_mappings_households_household_id",
                table: "vehicle_memo_mappings",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vehicles_households_household_id",
                table: "vehicles",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ynab_account_cache_households_household_id",
                table: "ynab_account_cache",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ynab_category_cache_households_household_id",
                table: "ynab_category_cache",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ynab_imports_households_household_id",
                table: "ynab_imports",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ynab_settings_households_household_id",
                table: "ynab_settings",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rolling back discards household membership. Re-creating the old global unique
            // indexes fails if two households hold the same memo name, category, or YNAB transaction.
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_users_households_household_id",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "fk_vehicle_memo_mappings_households_household_id",
                table: "vehicle_memo_mappings");

            migrationBuilder.DropForeignKey(
                name: "fk_vehicles_households_household_id",
                table: "vehicles");

            migrationBuilder.DropForeignKey(
                name: "fk_ynab_account_cache_households_household_id",
                table: "ynab_account_cache");

            migrationBuilder.DropForeignKey(
                name: "fk_ynab_category_cache_households_household_id",
                table: "ynab_category_cache");

            migrationBuilder.DropForeignKey(
                name: "fk_ynab_imports_households_household_id",
                table: "ynab_imports");

            migrationBuilder.DropForeignKey(
                name: "fk_ynab_settings_households_household_id",
                table: "ynab_settings");

            migrationBuilder.DropTable(
                name: "households");

            migrationBuilder.DropIndex(
                name: "ix_ynab_settings_household_id",
                table: "ynab_settings");

            migrationBuilder.DropIndex(
                name: "ix_ynab_imports_household_id_ynab_transaction_id",
                table: "ynab_imports");

            migrationBuilder.DropIndex(
                name: "ix_ynab_category_cache_household_id_category_id",
                table: "ynab_category_cache");

            migrationBuilder.DropIndex(
                name: "ix_ynab_account_cache_household_id",
                table: "ynab_account_cache");

            migrationBuilder.DropIndex(
                name: "ix_vehicles_household_id",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "ix_vehicle_memo_mappings_household_id_memo_name",
                table: "vehicle_memo_mappings");

            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_household_id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "ynab_settings");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "ynab_imports");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "ynab_category_cache");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "ynab_account_cache");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "vehicle_memo_mappings");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "household_role",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "ix_ynab_imports_ynab_transaction_id",
                table: "ynab_imports",
                column: "ynab_transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ynab_category_cache_category_id",
                table: "ynab_category_cache",
                column: "category_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_memo_mappings_memo_name",
                table: "vehicle_memo_mappings",
                column: "memo_name",
                unique: true);
        }
    }
}
