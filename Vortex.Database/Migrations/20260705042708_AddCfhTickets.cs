using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCfhTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "cfh_categories",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        name = table
                            .Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        display_order = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_cfh_categories", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "cfh_topics",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        category_id = table.Column<int>(type: "int", nullable: false),
                        name = table
                            .Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        consequence = table
                            .Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        default_sanction_preset_id = table.Column<int>(type: "int", nullable: true),
                        display_order = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_cfh_topics", x => x.id);
                        table.ForeignKey(
                            name: "FK_cfh_topics_cfh_categories_category_id",
                            column: x => x.category_id,
                            principalTable: "cfh_categories",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_cfh_topics_sanction_presets_default_sanction_preset_id",
                            column: x => x.default_sanction_preset_id,
                            principalTable: "sanction_presets",
                            principalColumn: "id"
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "cfh_tickets",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        state = table.Column<int>(type: "int", nullable: false),
                        topic_id = table.Column<int>(type: "int", nullable: false),
                        reporter_player_id = table.Column<int>(type: "int", nullable: false),
                        reported_player_id = table.Column<int>(type: "int", nullable: false),
                        room_id = table.Column<int>(type: "int", nullable: true),
                        message = table
                            .Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        evidence_json = table
                            .Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        picker_player_id = table.Column<int>(type: "int", nullable: true),
                        closed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        close_reason = table.Column<int>(type: "int", nullable: true),
                        sanctioned = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                        table.PrimaryKey("PK_cfh_tickets", x => x.id);
                        table.ForeignKey(
                            name: "FK_cfh_tickets_cfh_topics_topic_id",
                            column: x => x.topic_id,
                            principalTable: "cfh_topics",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_cfh_tickets_players_picker_player_id",
                            column: x => x.picker_player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Restrict
                        );
                        table.ForeignKey(
                            name: "FK_cfh_tickets_players_reported_player_id",
                            column: x => x.reported_player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Restrict
                        );
                        table.ForeignKey(
                            name: "FK_cfh_tickets_players_reporter_player_id",
                            column: x => x.reporter_player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Restrict
                        );
                        table.ForeignKey(
                            name: "FK_cfh_tickets_rooms_room_id",
                            column: x => x.room_id,
                            principalTable: "rooms",
                            principalColumn: "id"
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_cfh_tickets_picker_player_id",
                table: "cfh_tickets",
                column: "picker_player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_cfh_tickets_reported_player_id",
                table: "cfh_tickets",
                column: "reported_player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_cfh_tickets_reporter_player_id",
                table: "cfh_tickets",
                column: "reporter_player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_cfh_tickets_room_id",
                table: "cfh_tickets",
                column: "room_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_cfh_tickets_state",
                table: "cfh_tickets",
                column: "state"
            );

            migrationBuilder.CreateIndex(
                name: "IX_cfh_tickets_topic_id",
                table: "cfh_tickets",
                column: "topic_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_cfh_topics_category_id",
                table: "cfh_topics",
                column: "category_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_cfh_topics_default_sanction_preset_id",
                table: "cfh_topics",
                column: "default_sanction_preset_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "cfh_tickets");

            migrationBuilder.DropTable(name: "cfh_topics");

            migrationBuilder.DropTable(name: "cfh_categories");
        }
    }
}
