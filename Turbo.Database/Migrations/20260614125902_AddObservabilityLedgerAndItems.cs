using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddObservabilityLedgerAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "economy_ledger",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        player_id = table.Column<long>(type: "bigint", nullable: false),
                        currency = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        activity_point_type = table.Column<int>(type: "int", nullable: true),
                        delta = table.Column<long>(type: "bigint", nullable: false),
                        balance_after = table.Column<long>(type: "bigint", nullable: false),
                        reason = table
                            .Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        correlation_id = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ref_id = table.Column<long>(type: "bigint", nullable: true),
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
                        table.PrimaryKey("PK_economy_ledger", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "item_events",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        item_id = table.Column<long>(type: "bigint", nullable: false),
                        occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        event_type = table
                            .Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        actor_player_id = table.Column<long>(type: "bigint", nullable: true),
                        from_owner_id = table.Column<long>(type: "bigint", nullable: true),
                        to_owner_id = table.Column<long>(type: "bigint", nullable: true),
                        room_id = table.Column<int>(type: "int", nullable: true),
                        correlation_id = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
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
                        table.PrimaryKey("PK_item_events", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_economy_ledger_correlation_id",
                table: "economy_ledger",
                column: "correlation_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_economy_ledger_player_id_occurred_at",
                table: "economy_ledger",
                columns: new[] { "player_id", "occurred_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_item_events_correlation_id",
                table: "item_events",
                column: "correlation_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_item_events_item_id_occurred_at",
                table: "item_events",
                columns: new[] { "item_id", "occurred_at" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "economy_ledger");

            migrationBuilder.DropTable(name: "item_events");
        }
    }
}
