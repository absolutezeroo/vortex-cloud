using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCoveringIndexAndLtdSerialUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_furniture_player_id",
                table: "furniture");

            migrationBuilder.CreateIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_deleted_at",
                table: "furniture",
                columns: new[] { "player_id", "room_id", "wired_chest_id", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_ltd_raffle_entries_series_id_serial_number",
                table: "catalog_ltd_raffle_entries",
                columns: new[] { "series_id", "serial_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_deleted_at",
                table: "furniture");

            migrationBuilder.DropIndex(
                name: "IX_catalog_ltd_raffle_entries_series_id_serial_number",
                table: "catalog_ltd_raffle_entries");

            migrationBuilder.CreateIndex(
                name: "IX_furniture_player_id",
                table: "furniture",
                column: "player_id");
        }
    }
}
