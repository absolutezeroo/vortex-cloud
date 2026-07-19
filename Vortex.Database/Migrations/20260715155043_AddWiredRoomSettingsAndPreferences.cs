using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWiredRoomSettingsAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "wired_modify_permission_mask",
                table: "rooms",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "wired_read_permission_mask",
                table: "rooms",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder
                .AddColumn<string>(
                    name: "wired_timezone",
                    table: "rooms",
                    type: "varchar(64)",
                    maxLength: 64,
                    nullable: false,
                    defaultValue: ""
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "player_wired_preferences",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        wired_menu_button = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        wired_inspect_button = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        play_test_mode = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        wired_whisper_disabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        show_all_notifications = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        ui_style = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_player_wired_preferences", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_wired_preferences_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_player_wired_preferences_player_id",
                table: "player_wired_preferences",
                column: "player_id",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "player_wired_preferences");

            migrationBuilder.DropColumn(name: "wired_modify_permission_mask", table: "rooms");

            migrationBuilder.DropColumn(name: "wired_read_permission_mask", table: "rooms");

            migrationBuilder.DropColumn(name: "wired_timezone", table: "rooms");
        }
    }
}
