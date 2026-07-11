using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigatorPeripheralFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "score",
                table: "rooms",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<bool>(
                name: "staff_pick",
                table: "rooms",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder
                .AddColumn<string>(
                    name: "tag_1",
                    table: "rooms",
                    type: "varchar(25)",
                    maxLength: 25,
                    nullable: true
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AddColumn<string>(
                    name: "tag_2",
                    table: "rooms",
                    type: "varchar(25)",
                    maxLength: 25,
                    nullable: true
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "home_room_id",
                table: "player_navigator_preferences",
                type: "int",
                nullable: true
            );

            migrationBuilder
                .CreateTable(
                    name: "room_ratings",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        room_id = table.Column<int>(type: "int", nullable: false),
                        player_id = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_room_ratings", x => x.id);
                        table.ForeignKey(
                            name: "FK_room_ratings_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_room_ratings_rooms_room_id",
                            column: x => x.room_id,
                            principalTable: "rooms",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_room_ratings_player_id",
                table: "room_ratings",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_room_ratings_room_id_player_id",
                table: "room_ratings",
                columns: new[] { "room_id", "player_id" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "room_ratings");

            migrationBuilder.DropColumn(name: "score", table: "rooms");

            migrationBuilder.DropColumn(name: "staff_pick", table: "rooms");

            migrationBuilder.DropColumn(name: "tag_1", table: "rooms");

            migrationBuilder.DropColumn(name: "tag_2", table: "rooms");

            migrationBuilder.DropColumn(
                name: "home_room_id",
                table: "player_navigator_preferences"
            );
        }
    }
}
