using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "discord_allow_joining",
                table: "player_account_preferences",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "discord_hide_in_hidden_rooms",
                table: "player_account_preferences",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<int>(
                name: "discord_settings_version",
                table: "player_account_preferences",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<bool>(
                name: "discord_share_activity",
                table: "player_account_preferences",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "discord_show_habbo",
                table: "player_account_preferences",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "discord_allow_joining",
                table: "player_account_preferences"
            );

            migrationBuilder.DropColumn(
                name: "discord_hide_in_hidden_rooms",
                table: "player_account_preferences"
            );

            migrationBuilder.DropColumn(
                name: "discord_settings_version",
                table: "player_account_preferences"
            );

            migrationBuilder.DropColumn(
                name: "discord_share_activity",
                table: "player_account_preferences"
            );

            migrationBuilder.DropColumn(
                name: "discord_show_habbo",
                table: "player_account_preferences"
            );
        }
    }
}
