using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixGuildRoomUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_rooms_group_id", table: "rooms");

            migrationBuilder.DropIndex(name: "IX_groups_room_id", table: "groups");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_group_id",
                table: "rooms",
                column: "group_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_groups_room_id",
                table: "groups",
                column: "room_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_rooms_group_id", table: "rooms");

            migrationBuilder.DropIndex(name: "IX_groups_room_id", table: "groups");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_group_id",
                table: "rooms",
                column: "group_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_groups_room_id",
                table: "groups",
                column: "room_id",
                unique: true
            );
        }
    }
}
