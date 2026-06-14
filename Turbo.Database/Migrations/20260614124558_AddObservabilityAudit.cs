using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddObservabilityAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "audit_events",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        category = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        action = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        severity = table
                            .Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        result = table
                            .Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        correlation_id = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        actor_player_id = table.Column<long>(type: "bigint", nullable: true),
                        target_player_id = table.Column<long>(type: "bigint", nullable: true),
                        room_id = table.Column<int>(type: "int", nullable: true),
                        item_id = table.Column<long>(type: "bigint", nullable: true),
                        data = table
                            .Column<string>(type: "text", maxLength: 512, nullable: true)
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
                        table.PrimaryKey("PK_audit_events", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_actor_player_id_occurred_at",
                table: "audit_events",
                columns: new[] { "actor_player_id", "occurred_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_category_occurred_at",
                table: "audit_events",
                columns: new[] { "category", "occurred_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_correlation_id",
                table: "audit_events",
                column: "correlation_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_room_id_occurred_at",
                table: "audit_events",
                columns: new[] { "room_id", "occurred_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_target_player_id_occurred_at",
                table: "audit_events",
                columns: new[] { "target_player_id", "occurred_at" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "audit_events");
        }
    }
}
