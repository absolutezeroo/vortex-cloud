using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementResolutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "achievement_resolutions",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        achievement_id = table.Column<int>(type: "int", nullable: false),
                        target_level_offset = table.Column<int>(type: "int", nullable: false),
                        sort_order = table.Column<int>(type: "int", nullable: false),
                        enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                        table.PrimaryKey("PK_achievement_resolutions", x => x.id);
                        table.ForeignKey(
                            name: "FK_achievement_resolutions_achievements_achievement_id",
                            column: x => x.achievement_id,
                            principalTable: "achievements",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "player_achievement_resolutions",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        item_id = table.Column<int>(type: "int", nullable: false),
                        achievement_id = table.Column<int>(type: "int", nullable: false),
                        target_level = table.Column<int>(type: "int", nullable: false),
                        started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        ends_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        awarded_badge_code = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
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
                        table.PrimaryKey("PK_player_achievement_resolutions", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_achievement_resolutions_achievements_achievement_id",
                            column: x => x.achievement_id,
                            principalTable: "achievements",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_player_achievement_resolutions_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_resolutions_achievement_id",
                table: "achievement_resolutions",
                column: "achievement_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_achievement_resolutions_achievement_id",
                table: "player_achievement_resolutions",
                column: "achievement_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_achievement_resolutions_item_id",
                table: "player_achievement_resolutions",
                column: "item_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_achievement_resolutions_player_id",
                table: "player_achievement_resolutions",
                column: "player_id"
            );

            // The statue is inert without offers -- the client returns early on an empty list and
            // never even shows its window -- so the hotel starts with a usable set.
            migrationBuilder.Sql(Seeds.SeedScripts.Read("achievement_resolutions.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "achievement_resolutions");

            migrationBuilder.DropTable(name: "player_achievement_resolutions");
        }
    }
}
