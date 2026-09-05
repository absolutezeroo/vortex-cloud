using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardTrackTaskSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "reward_track_task_conditions");

            migrationBuilder
                .AddColumn<string>(
                    name: "captured_facts",
                    table: "player_reward_track_tasks",
                    type: "varchar(512)",
                    maxLength: 512,
                    nullable: false,
                    defaultValue: ""
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "current_step",
                table: "player_reward_track_tasks",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder
                .CreateTable(
                    name: "reward_track_task_steps",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        task_id = table.Column<int>(type: "int", nullable: false),
                        step_index = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_reward_track_task_steps", x => x.id);
                        table.ForeignKey(
                            name: "FK_reward_track_task_steps_reward_track_tasks_task_id",
                            column: x => x.task_id,
                            principalTable: "reward_track_tasks",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "reward_track_step_filters",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        step_id = table.Column<int>(type: "int", nullable: false),
                        sort_order = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        fact_key = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        @operator = table.Column<int>(
                            name: "operator",
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        value = table
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
                        table.PrimaryKey("PK_reward_track_step_filters", x => x.id);
                        table.ForeignKey(
                            name: "FK_reward_track_step_filters_reward_track_task_steps_step_id",
                            column: x => x.step_id,
                            principalTable: "reward_track_task_steps",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_step_filters_step_id",
                table: "reward_track_step_filters",
                column: "step_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_task_steps_action_code",
                table: "reward_track_task_steps",
                column: "action_code"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_task_steps_task_id_step_index",
                table: "reward_track_task_steps",
                columns: new[] { "task_id", "step_index" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "reward_track_step_filters");

            migrationBuilder.DropTable(name: "reward_track_task_steps");

            migrationBuilder.DropColumn(name: "captured_facts", table: "player_reward_track_tasks");

            migrationBuilder.DropColumn(name: "current_step", table: "player_reward_track_tasks");

            migrationBuilder
                .CreateTable(
                    name: "reward_track_task_conditions",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        task_id = table.Column<int>(type: "int", nullable: false),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        field = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        @operator = table.Column<int>(
                            name: "operator",
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        sort_order = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        value = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_reward_track_task_conditions", x => x.id);
                        table.ForeignKey(
                            name: "FK_reward_track_task_conditions_reward_track_tasks_task_id",
                            column: x => x.task_id,
                            principalTable: "reward_track_tasks",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_reward_track_task_conditions_task_id",
                table: "reward_track_task_conditions",
                column: "task_id"
            );
        }
    }
}
