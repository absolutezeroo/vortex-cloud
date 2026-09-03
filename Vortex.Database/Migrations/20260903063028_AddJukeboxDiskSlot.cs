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
            migrationBuilder.DropIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_deleted_at",
                table: "furniture"
            );

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_furniture_jukebox_id", table: "furniture");

            migrationBuilder.DropIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_jukebox_id_delete~",
                table: "furniture"
            );

            migrationBuilder.DropColumn(name: "jukebox_id", table: "furniture");

            migrationBuilder.CreateIndex(
                name: "IX_furniture_player_id_room_id_wired_chest_id_deleted_at",
                table: "furniture",
                columns: new[] { "player_id", "room_id", "wired_chest_id", "deleted_at" }
            );
        }
    }
}
