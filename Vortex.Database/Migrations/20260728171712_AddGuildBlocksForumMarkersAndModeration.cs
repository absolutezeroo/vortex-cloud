using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildBlocksForumMarkersAndModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "admin_operation_at",
                table: "group_forum_threads",
                type: "datetime(6)",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "admin_player_id",
                table: "group_forum_threads",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "view_count",
                table: "group_forum_settings",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "admin_operation_at",
                table: "group_forum_posts",
                type: "datetime(6)",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "admin_player_id",
                table: "group_forum_posts",
                type: "int",
                nullable: true
            );

            migrationBuilder
                .CreateTable(
                    name: "group_blocked_members",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        group_id = table.Column<int>(type: "int", nullable: false),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        blocked_by_player_id = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_group_blocked_members", x => x.id);
                        table.ForeignKey(
                            name: "FK_group_blocked_members_groups_group_id",
                            column: x => x.group_id,
                            principalTable: "groups",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_group_blocked_members_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "group_forum_read_markers",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        group_id = table.Column<int>(type: "int", nullable: false),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        read_message_count = table.Column<int>(
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
                        table.PrimaryKey("PK_group_forum_read_markers", x => x.id);
                        table.ForeignKey(
                            name: "FK_group_forum_read_markers_groups_group_id",
                            column: x => x.group_id,
                            principalTable: "groups",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_group_forum_read_markers_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_threads_admin_player_id",
                table: "group_forum_threads",
                column: "admin_player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_posts_admin_player_id",
                table: "group_forum_posts",
                column: "admin_player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_blocked_members_group_id_player_id",
                table: "group_blocked_members",
                columns: new[] { "group_id", "player_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_blocked_members_player_id",
                table: "group_blocked_members",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_read_markers_group_id_player_id",
                table: "group_forum_read_markers",
                columns: new[] { "group_id", "player_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_read_markers_player_id",
                table: "group_forum_read_markers",
                column: "player_id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_group_forum_posts_players_admin_player_id",
                table: "group_forum_posts",
                column: "admin_player_id",
                principalTable: "players",
                principalColumn: "id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_group_forum_threads_players_admin_player_id",
                table: "group_forum_threads",
                column: "admin_player_id",
                principalTable: "players",
                principalColumn: "id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_group_forum_posts_players_admin_player_id",
                table: "group_forum_posts"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_group_forum_threads_players_admin_player_id",
                table: "group_forum_threads"
            );

            migrationBuilder.DropTable(name: "group_blocked_members");

            migrationBuilder.DropTable(name: "group_forum_read_markers");

            migrationBuilder.DropIndex(
                name: "IX_group_forum_threads_admin_player_id",
                table: "group_forum_threads"
            );

            migrationBuilder.DropIndex(
                name: "IX_group_forum_posts_admin_player_id",
                table: "group_forum_posts"
            );

            migrationBuilder.DropColumn(name: "admin_operation_at", table: "group_forum_threads");

            migrationBuilder.DropColumn(name: "admin_player_id", table: "group_forum_threads");

            migrationBuilder.DropColumn(name: "view_count", table: "group_forum_settings");

            migrationBuilder.DropColumn(name: "admin_operation_at", table: "group_forum_posts");

            migrationBuilder.DropColumn(name: "admin_player_id", table: "group_forum_posts");
        }
    }
}
