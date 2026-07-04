using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemovePerformanceLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "performance_logs");

            // Pre-existing drift picked up by the model diff, unrelated to the performance_logs
            // removal above: FurnitureDefinitionEntity.StuffDataType's backing enum was already
            // `byte` in code (Turbo.Primitives/Furniture/StuffData/StuffDataType.cs) but the last
            // migration snapshot still had the column as `int` — nothing else changed it since, so
            // this migration also reconciles that gap rather than leaving it for a future diff to
            // rediscover.
            migrationBuilder.AlterColumn<byte>(
                name: "stuff_data_type",
                table: "furniture_definitions",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "stuff_data_type",
                table: "furniture_definitions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(byte),
                oldType: "tinyint unsigned",
                oldDefaultValue: (byte)0
            );

            migrationBuilder
                .CreateTable(
                    name: "performance_logs",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        average_frame_rate = table.Column<int>(type: "int", nullable: false),
                        browser = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        elapsed_time = table.Column<int>(type: "int", nullable: false),
                        flash_version = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        garbage_collections = table.Column<int>(type: "int", nullable: false),
                        ip_address = table
                            .Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        is_debugger = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        memory_usage = table.Column<int>(type: "int", nullable: false),
                        os = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        user_agent = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_performance_logs", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_performance_logs_elapsed_time",
                table: "performance_logs",
                column: "elapsed_time"
            );

            migrationBuilder.CreateIndex(
                name: "IX_performance_logs_ip_address",
                table: "performance_logs",
                column: "ip_address"
            );
        }
    }
}
