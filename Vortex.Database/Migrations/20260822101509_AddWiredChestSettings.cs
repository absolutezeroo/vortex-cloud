using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWiredChestSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "auto_lock",
                table: "wired_chests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "capacity",
                table: "wired_chests",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "chest_state",
                table: "wired_chests",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder
                .AddColumn<string>(
                    name: "description",
                    table: "wired_chests",
                    type: "varchar(512)",
                    maxLength: 512,
                    nullable: false,
                    defaultValue: ""
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "everyone_can_donate",
                table: "wired_chests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "everyone_can_open",
                table: "wired_chests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "locked",
                table: "wired_chests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder
                .AddColumn<string>(
                    name: "name",
                    table: "wired_chests",
                    type: "varchar(512)",
                    maxLength: 512,
                    nullable: false,
                    defaultValue: ""
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "notification_mode",
                table: "wired_chests",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<bool>(
                name: "notify_on_any_wired_transaction",
                table: "wired_chests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "notify_on_donation",
                table: "wired_chests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "notify_on_withdraw",
                table: "wired_chests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "notify_when_empty",
                table: "wired_chests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "notify_when_full",
                table: "wired_chests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "preview_amount",
                table: "wired_chests",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "preview_items",
                table: "wired_chests",
                type: "int",
                nullable: false,
                defaultValue: 0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "auto_lock", table: "wired_chests");

            migrationBuilder.DropColumn(name: "capacity", table: "wired_chests");

            migrationBuilder.DropColumn(name: "chest_state", table: "wired_chests");

            migrationBuilder.DropColumn(name: "description", table: "wired_chests");

            migrationBuilder.DropColumn(name: "everyone_can_donate", table: "wired_chests");

            migrationBuilder.DropColumn(name: "everyone_can_open", table: "wired_chests");

            migrationBuilder.DropColumn(name: "locked", table: "wired_chests");

            migrationBuilder.DropColumn(name: "name", table: "wired_chests");

            migrationBuilder.DropColumn(name: "notification_mode", table: "wired_chests");

            migrationBuilder.DropColumn(
                name: "notify_on_any_wired_transaction",
                table: "wired_chests"
            );

            migrationBuilder.DropColumn(name: "notify_on_donation", table: "wired_chests");

            migrationBuilder.DropColumn(name: "notify_on_withdraw", table: "wired_chests");

            migrationBuilder.DropColumn(name: "notify_when_empty", table: "wired_chests");

            migrationBuilder.DropColumn(name: "notify_when_full", table: "wired_chests");

            migrationBuilder.DropColumn(name: "preview_amount", table: "wired_chests");

            migrationBuilder.DropColumn(name: "preview_items", table: "wired_chests");
        }
    }
}
