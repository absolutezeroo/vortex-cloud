using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWiredChests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "wired_chest_id",
                table: "furniture",
                type: "int",
                nullable: true
            );

            migrationBuilder
                .CreateTable(
                    name: "wired_chests",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        furniture_id = table.Column<int>(type: "int", nullable: false),
                        credits = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        notifications_enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: true
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
                        table.PrimaryKey("PK_wired_chests", x => x.id);
                        table.ForeignKey(
                            name: "FK_wired_chests_furniture_furniture_id",
                            column: x => x.furniture_id,
                            principalTable: "furniture",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_furniture_wired_chest_id",
                table: "furniture",
                column: "wired_chest_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_wired_chests_furniture_id",
                table: "wired_chests",
                column: "furniture_id",
                unique: true
            );

            migrationBuilder.AddForeignKey(
                name: "FK_furniture_wired_chests_wired_chest_id",
                table: "furniture",
                column: "wired_chest_id",
                principalTable: "wired_chests",
                principalColumn: "id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_furniture_wired_chests_wired_chest_id",
                table: "furniture"
            );

            migrationBuilder.DropTable(name: "wired_chests");

            migrationBuilder.DropIndex(name: "IX_furniture_wired_chest_id", table: "furniture");

            migrationBuilder.DropColumn(name: "wired_chest_id", table: "furniture");
        }
    }
}
