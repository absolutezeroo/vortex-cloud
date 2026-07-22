using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddServerConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "server_config",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        config_key = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        config_value = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        description = table
                            .Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
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
                        table.PrimaryKey("PK_server_config", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_server_config_config_key",
                table: "server_config",
                column: "config_key",
                unique: true
            );

            // Default MOTD so a fresh install shows one on login; operators edit it live via the
            // config grain afterwards (id / timestamps are DB-generated).
            migrationBuilder.InsertData(
                table: "server_config",
                columns: new[] { "config_key", "config_value", "description" },
                values: new object[]
                {
                    "motd.lines",
                    "[\"Welcome to Vortex! Have fun and be nice to each other.\"]",
                    "Message-of-the-day lines (JSON array)",
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "server_config");
        }
    }
}
