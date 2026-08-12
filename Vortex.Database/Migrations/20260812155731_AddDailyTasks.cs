using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "daily_tasks",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        task_code = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        quest_type_code = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        is_bonus = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        image_version = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        catalog_name = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        required_repeats = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 1
                        ),
                        enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: true
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
                        table.PrimaryKey("PK_daily_tasks", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "daily_task_rewards",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        task_id = table.Column<int>(type: "int", nullable: false),
                        product_item_type_id = table.Column<short>(
                            type: "smallint",
                            nullable: false,
                            defaultValue: (short)0
                        ),
                        reward_type_id = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        extra_params = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        amount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                        table.PrimaryKey("PK_daily_task_rewards", x => x.id);
                        table.ForeignKey(
                            name: "FK_daily_task_rewards_daily_tasks_task_id",
                            column: x => x.task_id,
                            principalTable: "daily_tasks",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "player_daily_tasks",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        task_id = table.Column<int>(type: "int", nullable: false),
                        assigned_on = table.Column<DateOnly>(type: "date", nullable: false),
                        repeats = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        status = table.Column<int>(type: "int", nullable: false),
                        expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        claimed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                        table.PrimaryKey("PK_player_daily_tasks", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_daily_tasks_daily_tasks_task_id",
                            column: x => x.task_id,
                            principalTable: "daily_tasks",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Restrict
                        );
                        table.ForeignKey(
                            name: "FK_player_daily_tasks_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_daily_task_rewards_task_id",
                table: "daily_task_rewards",
                column: "task_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_daily_tasks_task_code",
                table: "daily_tasks",
                column: "task_code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_daily_tasks_player_id_assigned_on",
                table: "player_daily_tasks",
                columns: new[] { "player_id", "assigned_on" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_daily_tasks_player_id_task_id_assigned_on",
                table: "player_daily_tasks",
                columns: new[] { "player_id", "task_id", "assigned_on" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_daily_tasks_task_id",
                table: "player_daily_tasks",
                column: "task_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "daily_task_rewards");

            migrationBuilder.DropTable(name: "player_daily_tasks");

            migrationBuilder.DropTable(name: "daily_tasks");
        }
    }
}
