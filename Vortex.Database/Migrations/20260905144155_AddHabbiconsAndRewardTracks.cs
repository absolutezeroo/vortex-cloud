using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddHabbiconsAndRewardTracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "habbicon_id",
                table: "messenger_messages",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder
                .CreateTable(
                    name: "habbicon_collections",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        code = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        sort_order = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: true
                        ),
                        hidden = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        available_from = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: true
                        ),
                        available_until = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: true
                        ),
                        price_credits = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        price_activity_points = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        activity_point_type = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        campaign_code = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
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
                        table.PrimaryKey("PK_habbicon_collections", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "player_reward_track_claims",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        track_id = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        prize_id = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        claimed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        points_at_claim = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        granted_summary = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
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
                        table.PrimaryKey("PK_player_reward_track_claims", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_reward_track_claims_players_player_id",
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
                    name: "player_reward_track_tasks",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        track_id = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        task_id = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        progress_count = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        highest_paid_level_index = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: -1
                        ),
                        distinct_keys = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
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
                        table.PrimaryKey("PK_player_reward_track_tasks", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_reward_track_tasks_players_player_id",
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
                    name: "player_reward_tracks",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        track_id = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        points = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        premium_unlocked = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        premium_unlocked_at = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: true
                        ),
                        completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        content_version = table.Column<int>(
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
                        table.PrimaryKey("PK_player_reward_tracks", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_reward_tracks_players_player_id",
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
                    name: "reward_tracks",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        track_id = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        theme = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: "blue"
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        sort_order = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        starts_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        progress_ends_at = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: true
                        ),
                        claim_ends_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        unlock_kind = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        unlock_value = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        completion_policy = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        premium_enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        premium_boost_permille = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 1000
                        ),
                        premium_instant_points = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        premium_cost_credits = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        premium_cost_diamonds = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        content_version = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 1
                        ),
                        hidden = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        campaign_code = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
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
                        table.PrimaryKey("PK_reward_tracks", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "habbicons",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        code = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        collection_id = table.Column<int>(type: "int", nullable: false),
                        sort_order = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        is_collection_reward = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        price_credits = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        price_activity_points = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        activity_point_type = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: true
                        ),
                        available_from = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: true
                        ),
                        available_until = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: true
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
                        table.PrimaryKey("PK_habbicons", x => x.id);
                        table.ForeignKey(
                            name: "FK_habbicons_habbicon_collections_collection_id",
                            column: x => x.collection_id,
                            principalTable: "habbicon_collections",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "reward_track_prizes",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        reward_track_id = table.Column<int>(type: "int", nullable: false),
                        prize_id = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        required_points = table.Column<int>(type: "int", nullable: false),
                        premium = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        sort_order = table.Column<int>(
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
                        table.PrimaryKey("PK_reward_track_prizes", x => x.id);
                        table.ForeignKey(
                            name: "FK_reward_track_prizes_reward_tracks_reward_track_id",
                            column: x => x.reward_track_id,
                            principalTable: "reward_tracks",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "reward_track_tasks",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        reward_track_id = table.Column<int>(type: "int", nullable: false),
                        task_id = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        action_code = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        parameter = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        mode = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        premium = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        sort_order = table.Column<int>(
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
                        table.PrimaryKey("PK_reward_track_tasks", x => x.id);
                        table.ForeignKey(
                            name: "FK_reward_track_tasks_reward_tracks_reward_track_id",
                            column: x => x.reward_track_id,
                            principalTable: "reward_tracks",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "player_habbicons",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        habbicon_id = table.Column<int>(type: "int", nullable: false),
                        state = table.Column<int>(type: "int", nullable: false),
                        source = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        acquired_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        last_used_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                        table.PrimaryKey("PK_player_habbicons", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_habbicons_habbicons_habbicon_id",
                            column: x => x.habbicon_id,
                            principalTable: "habbicons",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_player_habbicons_players_player_id",
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
                    name: "reward_track_prize_rewards",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        prize_id = table.Column<int>(type: "int", nullable: false),
                        kind = table.Column<int>(type: "int", nullable: false),
                        reward_type_id = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        amount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                        extra_params = table
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
                        table.PrimaryKey("PK_reward_track_prize_rewards", x => x.id);
                        table.ForeignKey(
                            name: "FK_reward_track_prize_rewards_reward_track_prizes_prize_id",
                            column: x => x.prize_id,
                            principalTable: "reward_track_prizes",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "reward_track_task_levels",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        task_id = table.Column<int>(type: "int", nullable: false),
                        level_index = table.Column<int>(type: "int", nullable: false),
                        required_count = table.Column<int>(type: "int", nullable: false),
                        points_reward = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        premium = table.Column<bool>(
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
                        table.PrimaryKey("PK_reward_track_task_levels", x => x.id);
                        table.ForeignKey(
                            name: "FK_reward_track_task_levels_reward_track_tasks_task_id",
                            column: x => x.task_id,
                            principalTable: "reward_track_tasks",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_habbicon_collections_code",
                table: "habbicon_collections",
                column: "code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_habbicons_code",
                table: "habbicons",
                column: "code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_habbicons_collection_id",
                table: "habbicons",
                column: "collection_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_habbicons_habbicon_id",
                table: "player_habbicons",
                column: "habbicon_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_habbicons_player_id_habbicon_id",
                table: "player_habbicons",
                columns: new[] { "player_id", "habbicon_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_habbicons_player_id_last_used_at",
                table: "player_habbicons",
                columns: new[] { "player_id", "last_used_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_reward_track_claims_player_id_track_id_prize_id",
                table: "player_reward_track_claims",
                columns: new[] { "player_id", "track_id", "prize_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_reward_track_claims_track_id_prize_id",
                table: "player_reward_track_claims",
                columns: new[] { "track_id", "prize_id" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_reward_track_tasks_player_id_track_id_task_id",
                table: "player_reward_track_tasks",
                columns: new[] { "player_id", "track_id", "task_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_reward_tracks_player_id_track_id",
                table: "player_reward_tracks",
                columns: new[] { "player_id", "track_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_reward_tracks_track_id",
                table: "player_reward_tracks",
                column: "track_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_prize_rewards_prize_id",
                table: "reward_track_prize_rewards",
                column: "prize_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_prizes_reward_track_id_prize_id",
                table: "reward_track_prizes",
                columns: new[] { "reward_track_id", "prize_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_prizes_reward_track_id_required_points",
                table: "reward_track_prizes",
                columns: new[] { "reward_track_id", "required_points" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_task_levels_task_id_level_index",
                table: "reward_track_task_levels",
                columns: new[] { "task_id", "level_index" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_tasks_action_code",
                table: "reward_track_tasks",
                column: "action_code"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_tasks_reward_track_id_task_id",
                table: "reward_track_tasks",
                columns: new[] { "reward_track_id", "task_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_tracks_status",
                table: "reward_tracks",
                column: "status"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_tracks_track_id",
                table: "reward_tracks",
                column: "track_id",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "player_habbicons");

            migrationBuilder.DropTable(name: "player_reward_track_claims");

            migrationBuilder.DropTable(name: "player_reward_track_tasks");

            migrationBuilder.DropTable(name: "player_reward_tracks");

            migrationBuilder.DropTable(name: "reward_track_prize_rewards");

            migrationBuilder.DropTable(name: "reward_track_task_levels");

            migrationBuilder.DropTable(name: "habbicons");

            migrationBuilder.DropTable(name: "reward_track_prizes");

            migrationBuilder.DropTable(name: "reward_track_tasks");

            migrationBuilder.DropTable(name: "habbicon_collections");

            migrationBuilder.DropTable(name: "reward_tracks");

            migrationBuilder.DropColumn(name: "habbicon_id", table: "messenger_messages");
        }
    }
}
