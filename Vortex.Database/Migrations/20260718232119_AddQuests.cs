using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "quests",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        campaign_code = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        chain_code = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        localization_code = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        quest_type = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        total_steps = table.Column<int>(type: "int", nullable: false),
                        reward_type = table.Column<int>(type: "int", nullable: false),
                        reward_amount = table.Column<int>(type: "int", nullable: false),
                        catalog_page_name = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        image_version = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        sort_order = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        easy = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        seasonal = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        seasonal_seconds = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
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
                        table.PrimaryKey("PK_quests", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "player_quests",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        quest_id = table.Column<int>(type: "int", nullable: false),
                        completed_steps = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        accepted = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        completed = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        accepted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                        table.PrimaryKey("PK_player_quests", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_quests_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_player_quests_quests_quest_id",
                            column: x => x.quest_id,
                            principalTable: "quests",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Restrict
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_player_quests_player_id_quest_id",
                table: "player_quests",
                columns: new[] { "player_id", "quest_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_quests_quest_id",
                table: "player_quests",
                column: "quest_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_quests_campaign_code",
                table: "quests",
                column: "campaign_code"
            );

            // Seed the real evergreen "social" tutorial campaign (localization codes match the
            // client's external texts: quests.social.<code>.name/.desc). quest_type names the
            // progression trigger; RoomEntry + FriendListSize auto-advance today, the others await
            // their own triggers (chat/wave/dance/respect).
            migrationBuilder.InsertData(
                table: "quests",
                columns: new[]
                {
                    "id",
                    "campaign_code",
                    "chain_code",
                    "localization_code",
                    "quest_type",
                    "total_steps",
                    "reward_type",
                    "reward_amount",
                    "sort_order",
                    "easy",
                },
                values: new object[,]
                {
                    { 1, "social", "social", "ENTEROTHERSROOM", "RoomEntry", 1, 0, 10, 1, true },
                    { 2, "social", "social", "REQUESTFRIEND", "FriendListSize", 1, 0, 10, 2, true },
                    { 3, "social", "social", "CHATWITHSOMEONE", "Chat", 1, 0, 10, 3, true },
                    { 4, "social", "social", "WAVE", "Wave", 1, 0, 10, 4, true },
                    { 5, "social", "social", "DANCE", "Dance", 1, 0, 10, 5, true },
                    { 6, "social", "social", "GIVERESPECT", "RespectGiven", 1, 0, 10, 6, true },
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "player_quests");

            migrationBuilder.DropTable(name: "quests");
        }
    }
}
