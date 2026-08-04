using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Gives the four idle/door-tile/pet toggles the 701 room-settings dialog added a column each.
    /// </summary>
    /// <remarks>
    /// The client has always sent them back on save and the serializer has always answered - with
    /// constants, because there was nowhere to read from. So the boxes rendered, accepted a click,
    /// and reverted on the next open. The defaults below are exactly the constants they replace, so
    /// no existing room changes behaviour on upgrade.
    /// </remarks>
    public partial class PersistRoomIdleAndDoorTileSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "idle_autokick_enabled",
                table: "rooms",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "idle_autokick_timeout_seconds",
                table: "rooms",
                type: "int",
                nullable: false,
                defaultValue: 1800
            );

            migrationBuilder.AddColumn<bool>(
                name: "idle_sleep_enabled",
                table: "rooms",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<int>(
                name: "idle_sleep_timeout_seconds",
                table: "rooms",
                type: "int",
                nullable: false,
                defaultValue: 300
            );

            migrationBuilder.AddColumn<bool>(
                name: "leave_on_door_tile",
                table: "rooms",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "mute_all_pets",
                table: "rooms",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "idle_autokick_enabled", table: "rooms");

            migrationBuilder.DropColumn(name: "idle_autokick_timeout_seconds", table: "rooms");

            migrationBuilder.DropColumn(name: "idle_sleep_enabled", table: "rooms");

            migrationBuilder.DropColumn(name: "idle_sleep_timeout_seconds", table: "rooms");

            migrationBuilder.DropColumn(name: "leave_on_door_tile", table: "rooms");

            migrationBuilder.DropColumn(name: "mute_all_pets", table: "rooms");
        }
    }
}
