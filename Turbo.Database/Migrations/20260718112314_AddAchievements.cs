using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "achievement_score",
                table: "players",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder
                .CreateTable(
                    name: "achievements",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        name = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        category = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        display_method = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_achievements", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "achievement_levels",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        achievement_id = table.Column<int>(type: "int", nullable: false),
                        level = table.Column<int>(type: "int", nullable: false),
                        badge_code = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        progress_requirement = table.Column<int>(type: "int", nullable: false),
                        reward_amount = table.Column<int>(type: "int", nullable: false),
                        reward_type = table.Column<int>(type: "int", nullable: false),
                        score_points = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_achievement_levels", x => x.id);
                        table.ForeignKey(
                            name: "FK_achievement_levels_achievements_achievement_id",
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
                    name: "player_achievements",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        achievement_id = table.Column<int>(type: "int", nullable: false),
                        progress = table.Column<int>(type: "int", nullable: false),
                        level = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_player_achievements", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_achievements_achievements_achievement_id",
                            column: x => x.achievement_id,
                            principalTable: "achievements",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Restrict
                        );
                        table.ForeignKey(
                            name: "FK_player_achievements_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_achievement_id_level",
                table: "achievement_levels",
                columns: new[] { "achievement_id", "level" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_achievements_name",
                table: "achievements",
                column: "name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_achievements_achievement_id",
                table: "player_achievements",
                column: "achievement_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_achievements_player_id_achievement_id",
                table: "player_achievements",
                columns: new[] { "player_id", "achievement_id" },
                unique: true
            );

            // Seed a core set of achievements (headers + 5 cumulative levels each). Badge codes
            // follow Habbo's "ACH_" + name + level convention; thresholds/rewards are DB-editable.
            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "id", "name", "category", "display_method" },
                values: new object[,]
                {
                    { 1, "RoomEntry", "explore", 0 },
                    { 2, "Login", "explore", 0 },
                    { 3, "AllTimeHotelPresence", "explore", 0 },
                    { 4, "Motto", "avatar", 0 },
                    { 5, "AvatarLooks", "avatar", 0 },
                    { 6, "FriendCount", "social", 0 },
                    { 7, "RespectGiven", "social", 0 },
                    { 8, "RespectEarned", "social", 0 },
                    { 9, "RoomDecoFurniCount", "build", 0 },
                    { 10, "GamePlayed", "games", 0 },
                }
            );

            migrationBuilder.InsertData(
                table: "achievement_levels",
                columns: new[]
                {
                    "id",
                    "achievement_id",
                    "level",
                    "badge_code",
                    "progress_requirement",
                    "reward_amount",
                    "reward_type",
                    "score_points",
                },
                values: new object[,]
                {
                    { 1, 1, 1, "ACH_RoomEntry1", 1, 10, 0, 1 },
                    { 2, 1, 2, "ACH_RoomEntry2", 10, 20, 0, 2 },
                    { 3, 1, 3, "ACH_RoomEntry3", 25, 30, 0, 3 },
                    { 4, 1, 4, "ACH_RoomEntry4", 50, 40, 0, 4 },
                    { 5, 1, 5, "ACH_RoomEntry5", 100, 50, 0, 5 },
                    { 6, 2, 1, "ACH_Login1", 1, 10, 0, 1 },
                    { 7, 2, 2, "ACH_Login2", 5, 20, 0, 2 },
                    { 8, 2, 3, "ACH_Login3", 15, 30, 0, 3 },
                    { 9, 2, 4, "ACH_Login4", 30, 40, 0, 4 },
                    { 10, 2, 5, "ACH_Login5", 60, 50, 0, 5 },
                    { 11, 3, 1, "ACH_AllTimeHotelPresence1", 1, 10, 0, 1 },
                    { 12, 3, 2, "ACH_AllTimeHotelPresence2", 5, 20, 0, 2 },
                    { 13, 3, 3, "ACH_AllTimeHotelPresence3", 24, 30, 0, 3 },
                    { 14, 3, 4, "ACH_AllTimeHotelPresence4", 72, 40, 0, 4 },
                    { 15, 3, 5, "ACH_AllTimeHotelPresence5", 168, 50, 0, 5 },
                    { 16, 4, 1, "ACH_Motto1", 1, 10, 0, 1 },
                    { 17, 4, 2, "ACH_Motto2", 2, 20, 0, 2 },
                    { 18, 4, 3, "ACH_Motto3", 3, 30, 0, 3 },
                    { 19, 4, 4, "ACH_Motto4", 4, 40, 0, 4 },
                    { 20, 4, 5, "ACH_Motto5", 5, 50, 0, 5 },
                    { 21, 5, 1, "ACH_AvatarLooks1", 1, 10, 0, 1 },
                    { 22, 5, 2, "ACH_AvatarLooks2", 3, 20, 0, 2 },
                    { 23, 5, 3, "ACH_AvatarLooks3", 6, 30, 0, 3 },
                    { 24, 5, 4, "ACH_AvatarLooks4", 10, 40, 0, 4 },
                    { 25, 5, 5, "ACH_AvatarLooks5", 15, 50, 0, 5 },
                    { 26, 6, 1, "ACH_FriendCount1", 1, 10, 0, 1 },
                    { 27, 6, 2, "ACH_FriendCount2", 5, 20, 0, 2 },
                    { 28, 6, 3, "ACH_FriendCount3", 10, 30, 0, 3 },
                    { 29, 6, 4, "ACH_FriendCount4", 25, 40, 0, 4 },
                    { 30, 6, 5, "ACH_FriendCount5", 50, 50, 0, 5 },
                    { 31, 7, 1, "ACH_RespectGiven1", 1, 10, 0, 1 },
                    { 32, 7, 2, "ACH_RespectGiven2", 10, 20, 0, 2 },
                    { 33, 7, 3, "ACH_RespectGiven3", 25, 30, 0, 3 },
                    { 34, 7, 4, "ACH_RespectGiven4", 50, 40, 0, 4 },
                    { 35, 7, 5, "ACH_RespectGiven5", 100, 50, 0, 5 },
                    { 36, 8, 1, "ACH_RespectEarned1", 1, 10, 0, 1 },
                    { 37, 8, 2, "ACH_RespectEarned2", 10, 20, 0, 2 },
                    { 38, 8, 3, "ACH_RespectEarned3", 25, 30, 0, 3 },
                    { 39, 8, 4, "ACH_RespectEarned4", 50, 40, 0, 4 },
                    { 40, 8, 5, "ACH_RespectEarned5", 100, 50, 0, 5 },
                    { 41, 9, 1, "ACH_RoomDecoFurniCount1", 1, 10, 0, 1 },
                    { 42, 9, 2, "ACH_RoomDecoFurniCount2", 10, 20, 0, 2 },
                    { 43, 9, 3, "ACH_RoomDecoFurniCount3", 50, 30, 0, 3 },
                    { 44, 9, 4, "ACH_RoomDecoFurniCount4", 100, 40, 0, 4 },
                    { 45, 9, 5, "ACH_RoomDecoFurniCount5", 250, 50, 0, 5 },
                    { 46, 10, 1, "ACH_GamePlayed1", 1, 10, 0, 1 },
                    { 47, 10, 2, "ACH_GamePlayed2", 5, 20, 0, 2 },
                    { 48, 10, 3, "ACH_GamePlayed3", 15, 30, 0, 3 },
                    { 49, 10, 4, "ACH_GamePlayed4", 30, 40, 0, 4 },
                    { 50, 10, 5, "ACH_GamePlayed5", 60, 50, 0, 5 },
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "achievement_levels");

            migrationBuilder.DropTable(name: "player_achievements");

            migrationBuilder.DropTable(name: "achievements");

            migrationBuilder.DropColumn(name: "achievement_score", table: "players");
        }
    }
}
