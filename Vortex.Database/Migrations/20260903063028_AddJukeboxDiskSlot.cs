using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddJukeboxDiskSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "jukebox_id",
                table: "furniture",
                type: "int",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_furniture_jukebox_id",
                table: "furniture",
                column: "jukebox_id"
            );

            // Create before drop, not the other way round -- the same order WidenPlayerFigure had to
            // use, for the same reason. furniture.player_id carries a foreign key and MySQL insists a
            // foreign key keeps an index; the old composite was the only one starting with that
            // column, so dropping it first fails outright with "needed in a foreign key constraint".
            // The replacement starts with player_id too, so once it exists the old one is free to go.
            migrationBuilder.CreateIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_jukebox_id_delete~",
                table: "furniture",
                columns: new[]
                {
                    "player_id",
                    "room_id",
                    "wired_chest_id",
                    "jukebox_id",
                    "deleted_at",
                }
            );

            migrationBuilder.DropIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_deleted_at",
                table: "furniture"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create before drop here too, and for the same foreign key.
            migrationBuilder.CreateIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_deleted_at",
                table: "furniture",
                columns: new[] { "player_id", "room_id", "wired_chest_id", "deleted_at" }
            );

            migrationBuilder.DropIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_jukebox_id_delete~",
                table: "furniture"
            );

            migrationBuilder.DropIndex(name: "IX_furniture_jukebox_id", table: "furniture");

            migrationBuilder.DropColumn(name: "jukebox_id", table: "furniture");
        }
    }
}
