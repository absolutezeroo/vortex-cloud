using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "bots",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        room_id = table.Column<int>(type: "int", nullable: true),
                        name = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        motto = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        figure = table
                            .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        gender = table.Column<int>(type: "int", nullable: false),
                        x = table.Column<int>(type: "int", nullable: false),
                        y = table.Column<int>(type: "int", nullable: false),
                        z = table.Column<int>(type: "int", nullable: false),
                        rotation = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_bots", x => x.id);
                        table.ForeignKey(
                            name: "FK_bots_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_bots_rooms_room_id",
                            column: x => x.room_id,
                            principalTable: "rooms",
                            principalColumn: "id"
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_bots_player_id",
                table: "bots",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(name: "IX_bots_room_id", table: "bots", column: "room_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bots");
        }
    }
}
