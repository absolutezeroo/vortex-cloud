using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Context;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TurboDbContext))]
    [Migration("20260614200000_AddObservabilityErrorTracking")]
    public partial class AddObservabilityErrorTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "error_groups",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        fingerprint = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        source = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        operation = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        exception_type = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        message_signature = table
                            .Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        sample_message = table
                            .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        first_seen_at = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: false
                        ),
                        last_seen_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        total_occurrences = table.Column<int>(type: "int", nullable: false),
                        last_actor_player_id = table.Column<long>(type: "bigint", nullable: true),
                        last_room_id = table.Column<int>(type: "int", nullable: true),
                        last_correlation_id = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_error_groups", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "error_occurrences",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        fingerprint = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        group_id = table.Column<int>(type: "int", nullable: false),
                        occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        source = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        operation = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        exception_type = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        message = table
                            .Column<string>(type: "text", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        stack_trace = table
                            .Column<string>(type: "text", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        correlation_id = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        actor_player_id = table.Column<long>(type: "bigint", nullable: true),
                        room_id = table.Column<int>(type: "int", nullable: true),
                        session_key = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        remote_ip = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_error_occurrences", x => x.id);
                        table.ForeignKey(
                            name: "FK_error_occurrences_error_groups_group_id",
                            column: x => x.group_id,
                            principalTable: "error_groups",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_error_groups_fingerprint",
                table: "error_groups",
                column: "fingerprint",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_error_groups_last_seen_at",
                table: "error_groups",
                column: "last_seen_at"
            );

            migrationBuilder.CreateIndex(
                name: "IX_error_groups_source_operation_exception_type",
                table: "error_groups",
                columns: new[] { "source", "operation", "exception_type" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_error_occurrences_fingerprint",
                table: "error_occurrences",
                column: "fingerprint"
            );

            migrationBuilder.CreateIndex(
                name: "IX_error_occurrences_group_id",
                table: "error_occurrences",
                column: "group_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_error_occurrences_occurred_at",
                table: "error_occurrences",
                column: "occurred_at"
            );

            migrationBuilder.CreateIndex(
                name: "IX_error_occurrences_source_occurred_at",
                table: "error_occurrences",
                columns: new[] { "source", "occurred_at" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "error_occurrences");

            migrationBuilder.DropTable(name: "error_groups");
        }
    }
}
