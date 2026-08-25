using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceOperationJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "commerce_operations",
                    columns: table => new
                    {
                        id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        kind = table.Column<int>(type: "int", nullable: false),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        state = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        current_step = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        last_error = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        detail = table
                            .Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        pivoted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_commerce_operations", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "commerce_receipts",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        operation_id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        step_key = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        result = table
                            .Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_commerce_receipts", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_operations_player_id",
                table: "commerce_operations",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_commerce_operations_state_pivoted_at",
                table: "commerce_operations",
                columns: new[] { "state", "pivoted_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_commerce_receipts_operation_id_step_key",
                table: "commerce_receipts",
                columns: new[] { "operation_id", "step_key" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "commerce_operations");

            migrationBuilder.DropTable(name: "commerce_receipts");
        }
    }
}
