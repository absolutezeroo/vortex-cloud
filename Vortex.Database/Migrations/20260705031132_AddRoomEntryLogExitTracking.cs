using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomEntryLogExitTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "exited_at",
                table: "room_entry_logs",
                type: "datetime(6)",
                nullable: true
            );

            // Create the replacement composite index before dropping the old one:
            // MySQL requires an index covering `player_id` at all times to satisfy
            // the FK constraint, and the composite index's leftmost column covers it.
            migrationBuilder.CreateIndex(
                name: "IX_room_entry_logs_player_id_room_id_exited_at",
                table: "room_entry_logs",
                columns: new[] { "player_id", "room_id", "exited_at" }
            );

            migrationBuilder.DropIndex(
                name: "IX_room_entry_logs_player_id",
                table: "room_entry_logs"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_room_entry_logs_player_id",
                table: "room_entry_logs",
                column: "player_id"
            );

            migrationBuilder.DropIndex(
                name: "IX_room_entry_logs_player_id_room_id_exited_at",
                table: "room_entry_logs"
            );

            migrationBuilder.DropColumn(name: "exited_at", table: "room_entry_logs");
        }
    }
}
