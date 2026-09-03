using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSongs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "songs",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        name = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        creator = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        length_ms = table.Column<int>(type: "int", nullable: false),
                        official_song_id = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        data = table
                            .Column<string>(type: "text", maxLength: 512, nullable: false)
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
                        table.PrimaryKey("PK_songs", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_songs_official_song_id",
                table: "songs",
                column: "official_song_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "songs");
        }
    }
}
