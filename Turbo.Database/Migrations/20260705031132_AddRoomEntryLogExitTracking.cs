using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomEntryLogExitTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_room_entry_logs_player_id",
                table: "room_entry_logs"
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "exited_at",
                table: "room_entry_logs",
                type: "datetime(6)",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_room_entry_logs_player_id_room_id_exited_at",
                table: "room_entry_logs",
                columns: new[] { "player_id", "room_id", "exited_at" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_room_entry_logs_player_id_room_id_exited_at",
                table: "room_entry_logs"
            );

            migrationBuilder.DropColumn(name: "exited_at", table: "room_entry_logs");

            migrationBuilder.CreateIndex(
                name: "IX_room_entry_logs_player_id",
                table: "room_entry_logs",
                column: "player_id"
            );
        }
    }
}
