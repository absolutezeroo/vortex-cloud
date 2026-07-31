using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPetCommandNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "pet_command_names",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        command = table.Column<int>(type: "int", nullable: false),
                        name = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
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
                        table.PrimaryKey("PK_pet_command_names", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_pet_command_names_command",
                table: "pet_command_names",
                column: "command",
                unique: true
            );

            migrationBuilder.Sql(SeedScripts.Read("pet_command_names.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "pet_command_names");
        }
    }
}
