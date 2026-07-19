using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameLtdTablesToCatalogPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ltd_raffle_entries_ltd_series_series_id",
                table: "ltd_raffle_entries"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_ltd_raffle_entries_players_player_id",
                table: "ltd_raffle_entries"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_ltd_series_catalog_products_catalog_product_id",
                table: "ltd_series"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_ltd_raffle_entries",
                table: "ltd_raffle_entries"
            );

            migrationBuilder.DropPrimaryKey(name: "PK_ltd_series", table: "ltd_series");

            migrationBuilder.RenameTable(name: "ltd_series", newName: "catalog_ltd_series");

            migrationBuilder.RenameIndex(
                name: "IX_ltd_series_catalog_product_id",
                table: "catalog_ltd_series",
                newName: "IX_catalog_ltd_series_catalog_product_id"
            );

            migrationBuilder.RenameTable(
                name: "ltd_raffle_entries",
                newName: "catalog_ltd_raffle_entries"
            );

            migrationBuilder.RenameIndex(
                name: "IX_ltd_raffle_entries_player_id",
                table: "catalog_ltd_raffle_entries",
                newName: "IX_catalog_ltd_raffle_entries_player_id"
            );

            migrationBuilder.RenameIndex(
                name: "IX_ltd_raffle_entries_series_id",
                table: "catalog_ltd_raffle_entries",
                newName: "IX_catalog_ltd_raffle_entries_series_id"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_catalog_ltd_series",
                table: "catalog_ltd_series",
                column: "id"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_catalog_ltd_raffle_entries",
                table: "catalog_ltd_raffle_entries",
                column: "id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_ltd_series_catalog_products_catalog_product_id",
                table: "catalog_ltd_series",
                column: "catalog_product_id",
                principalTable: "catalog_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_ltd_raffle_entries_catalog_ltd_series_series_id",
                table: "catalog_ltd_raffle_entries",
                column: "series_id",
                principalTable: "catalog_ltd_series",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_ltd_raffle_entries_players_player_id",
                table: "catalog_ltd_raffle_entries",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_ltd_raffle_entries_catalog_ltd_series_series_id",
                table: "catalog_ltd_raffle_entries"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_ltd_raffle_entries_players_player_id",
                table: "catalog_ltd_raffle_entries"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_ltd_series_catalog_products_catalog_product_id",
                table: "catalog_ltd_series"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_catalog_ltd_raffle_entries",
                table: "catalog_ltd_raffle_entries"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_catalog_ltd_series",
                table: "catalog_ltd_series"
            );

            migrationBuilder.RenameTable(
                name: "catalog_ltd_raffle_entries",
                newName: "ltd_raffle_entries"
            );

            migrationBuilder.RenameIndex(
                name: "IX_catalog_ltd_raffle_entries_player_id",
                table: "ltd_raffle_entries",
                newName: "IX_ltd_raffle_entries_player_id"
            );

            migrationBuilder.RenameIndex(
                name: "IX_catalog_ltd_raffle_entries_series_id",
                table: "ltd_raffle_entries",
                newName: "IX_ltd_raffle_entries_series_id"
            );

            migrationBuilder.RenameTable(name: "catalog_ltd_series", newName: "ltd_series");

            migrationBuilder.RenameIndex(
                name: "IX_catalog_ltd_series_catalog_product_id",
                table: "ltd_series",
                newName: "IX_ltd_series_catalog_product_id"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_ltd_series",
                table: "ltd_series",
                column: "id"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_ltd_raffle_entries",
                table: "ltd_raffle_entries",
                column: "id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_ltd_series_catalog_products_catalog_product_id",
                table: "ltd_series",
                column: "catalog_product_id",
                principalTable: "catalog_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_ltd_raffle_entries_ltd_series_series_id",
                table: "ltd_raffle_entries",
                column: "series_id",
                principalTable: "ltd_series",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_ltd_raffle_entries_players_player_id",
                table: "ltd_raffle_entries",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
