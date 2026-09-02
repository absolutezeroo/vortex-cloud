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
            // Create before dropping, which is the reverse of what EF scaffolds. The foreign key on
            // player_id needs *an* index leading with that column; the composite below is one, but
            // only once it exists. Dropping first leaves the constraint momentarily uncovered and
            // MySQL refuses outright: "Cannot drop index 'IX_furniture_player_id': needed in a
            // foreign key constraint".
            migrationBuilder.CreateIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_deleted_at",
                table: "furniture",
                columns: new[] { "player_id", "room_id", "wired_chest_id", "deleted_at" }
            );

            migrationBuilder.DropIndex(name: "IX_furniture_player_id", table: "furniture");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_ltd_raffle_entries_series_id_serial_number",
                table: "catalog_ltd_raffle_entries",
                columns: new[] { "series_id", "serial_number" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Same reason as Up, the other way round: the simple index has to be back before the
            // composite one that is currently covering the foreign key can go.
            migrationBuilder.CreateIndex(
                name: "IX_furniture_player_id",
                table: "furniture",
                column: "player_id"
            );

            migrationBuilder.DropIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_deleted_at",
                table: "furniture"
            );

            migrationBuilder.DropIndex(
                name: "IX_catalog_ltd_raffle_entries_series_id_serial_number",
                table: "catalog_ltd_raffle_entries"
            );
        }
    }
}
