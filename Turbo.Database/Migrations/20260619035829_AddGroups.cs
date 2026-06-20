using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "group_id",
                table: "rooms",
                type: "int",
                nullable: true
            );

            migrationBuilder
                .CreateTable(
                    name: "groups",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        name = table
                            .Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        description = table
                            .Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        badge = table
                            .Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        room_id = table.Column<int>(type: "int", nullable: false),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        type = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        color_one = table
                            .Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        color_two = table
                            .Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        admin_only_decoration = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
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
                        table.PrimaryKey("PK_groups", x => x.id);
                        table.ForeignKey(
                            name: "FK_groups_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_groups_rooms_room_id",
                            column: x => x.room_id,
                            principalTable: "rooms",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Restrict
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "group_forum_settings",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        group_id = table.Column<int>(type: "int", nullable: false),
                        enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        read_permission = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        post_permission = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        thread_permission = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        mod_permission = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 1
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
                        table.PrimaryKey("PK_group_forum_settings", x => x.id);
                        table.ForeignKey(
                            name: "FK_group_forum_settings_groups_group_id",
                            column: x => x.group_id,
                            principalTable: "groups",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "group_forum_threads",
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
                        subject = table
                            .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        state = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        is_pinned = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        post_count = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        last_post_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        last_post_player_id = table.Column<int>(type: "int", nullable: true),
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
                        table.PrimaryKey("PK_group_forum_threads", x => x.id);
                        table.ForeignKey(
                            name: "FK_group_forum_threads_groups_group_id",
                            column: x => x.group_id,
                            principalTable: "groups",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_group_forum_threads_players_last_post_player_id",
                            column: x => x.last_post_player_id,
                            principalTable: "players",
                            principalColumn: "id"
                        );
                        table.ForeignKey(
                            name: "FK_group_forum_threads_players_player_id",
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
                    name: "group_members",
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
                        rank = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                        table.PrimaryKey("PK_group_members", x => x.id);
                        table.ForeignKey(
                            name: "FK_group_members_groups_group_id",
                            column: x => x.group_id,
                            principalTable: "groups",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_group_members_players_player_id",
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
                    name: "group_membership_requests",
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
                        table.PrimaryKey("PK_group_membership_requests", x => x.id);
                        table.ForeignKey(
                            name: "FK_group_membership_requests_groups_group_id",
                            column: x => x.group_id,
                            principalTable: "groups",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_group_membership_requests_players_player_id",
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
                    name: "group_forum_posts",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        thread_id = table.Column<int>(type: "int", nullable: false),
                        group_id = table.Column<int>(type: "int", nullable: false),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        message = table
                            .Column<string>(type: "text", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        state = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                        table.PrimaryKey("PK_group_forum_posts", x => x.id);
                        table.ForeignKey(
                            name: "FK_group_forum_posts_group_forum_threads_thread_id",
                            column: x => x.thread_id,
                            principalTable: "group_forum_threads",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_group_forum_posts_groups_group_id",
                            column: x => x.group_id,
                            principalTable: "groups",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_group_forum_posts_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_group_id",
                table: "rooms",
                column: "group_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_posts_group_id",
                table: "group_forum_posts",
                column: "group_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_posts_player_id",
                table: "group_forum_posts",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_posts_thread_id",
                table: "group_forum_posts",
                column: "thread_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_settings_group_id",
                table: "group_forum_settings",
                column: "group_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_threads_group_id",
                table: "group_forum_threads",
                column: "group_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_threads_last_post_at",
                table: "group_forum_threads",
                column: "last_post_at"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_threads_last_post_player_id",
                table: "group_forum_threads",
                column: "last_post_player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_forum_threads_player_id",
                table: "group_forum_threads",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_members_group_id_player_id",
                table: "group_members",
                columns: new[] { "group_id", "player_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_members_player_id",
                table: "group_members",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_membership_requests_group_id_player_id",
                table: "group_membership_requests",
                columns: new[] { "group_id", "player_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_membership_requests_player_id",
                table: "group_membership_requests",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_groups_player_id",
                table: "groups",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_groups_room_id",
                table: "groups",
                column: "room_id",
                unique: true
            );

            migrationBuilder.AddForeignKey(
                name: "FK_rooms_groups_group_id",
                table: "rooms",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_rooms_groups_group_id", table: "rooms");

            migrationBuilder.DropTable(name: "group_forum_posts");

            migrationBuilder.DropTable(name: "group_forum_settings");

            migrationBuilder.DropTable(name: "group_members");

            migrationBuilder.DropTable(name: "group_membership_requests");

            migrationBuilder.DropTable(name: "group_forum_threads");

            migrationBuilder.DropTable(name: "groups");

            migrationBuilder.DropIndex(name: "IX_rooms_group_id", table: "rooms");

            migrationBuilder.DropColumn(name: "group_id", table: "rooms");
        }
    }
}
