using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerChatDisplayPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "chat_bubble_width",
                table: "player_account_preferences",
                type: "int",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.AddColumn<int>(
                name: "chat_mode",
                table: "player_account_preferences",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "chat_scroll_speed",
                table: "player_account_preferences",
                type: "int",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.AddColumn<int>(
                name: "chat_size_preference",
                table: "player_account_preferences",
                type: "int",
                nullable: false,
                defaultValue: 0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chat_bubble_width",
                table: "player_account_preferences"
            );

            migrationBuilder.DropColumn(name: "chat_mode", table: "player_account_preferences");

            migrationBuilder.DropColumn(
                name: "chat_scroll_speed",
                table: "player_account_preferences"
            );

            migrationBuilder.DropColumn(
                name: "chat_size_preference",
                table: "player_account_preferences"
            );
        }
    }
}
