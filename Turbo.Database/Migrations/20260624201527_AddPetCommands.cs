using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPetCommands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "pet_commands",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        pet_type = table.Column<int>(type: "int", nullable: false),
                        command = table.Column<int>(type: "int", nullable: false),
                        level_required = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_pet_commands", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_pet_commands_pet_type",
                table: "pet_commands",
                column: "pet_type"
            );

            migrationBuilder.CreateIndex(
                name: "IX_pet_commands_pet_type_command",
                table: "pet_commands",
                columns: new[] { "pet_type", "command" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "pet_commands");
        }
    }
}
