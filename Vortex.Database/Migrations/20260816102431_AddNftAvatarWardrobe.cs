using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddNftAvatarWardrobe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "nft_avatars",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        avatar_code = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        name = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        figure = table
                            .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        gender = table
                            .Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        contract_key = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        edition_size = table.Column<int>(type: "int", nullable: false),
                        enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        sort_order = table.Column<int>(type: "int", nullable: false),
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
                        table.PrimaryKey("PK_nft_avatars", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "player_nft_avatars",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        nft_avatar_id = table.Column<int>(type: "int", nullable: false),
                        serial_number = table.Column<int>(type: "int", nullable: false),
                        granted_by_player_id = table.Column<int>(type: "int", nullable: true),
                        grant_note = table
                            .Column<string>(type: "varchar(190)", maxLength: 190, nullable: false)
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
                        table.PrimaryKey("PK_player_nft_avatars", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_nft_avatars_nft_avatars_nft_avatar_id",
                            column: x => x.nft_avatar_id,
                            principalTable: "nft_avatars",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_player_nft_avatars_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "player_nft_outfit",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        player_nft_avatar_id = table.Column<int>(type: "int", nullable: false),
                        fallback_figure = table
                            .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        fallback_gender = table
                            .Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
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
                        table.PrimaryKey("PK_player_nft_outfit", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_nft_outfit_player_nft_avatars_player_nft_avatar_id",
                            column: x => x.player_nft_avatar_id,
                            principalTable: "player_nft_avatars",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_player_nft_outfit_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_nft_avatars_avatar_code",
                table: "nft_avatars",
                column: "avatar_code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_nft_avatars_nft_avatar_id_serial_number",
                table: "player_nft_avatars",
                columns: new[] { "nft_avatar_id", "serial_number" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_nft_avatars_player_id_nft_avatar_id",
                table: "player_nft_avatars",
                columns: new[] { "player_id", "nft_avatar_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_nft_outfit_player_id",
                table: "player_nft_outfit",
                column: "player_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_nft_outfit_player_nft_avatar_id",
                table: "player_nft_outfit",
                column: "player_nft_avatar_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "player_nft_outfit");

            migrationBuilder.DropTable(name: "player_nft_avatars");

            migrationBuilder.DropTable(name: "nft_avatars");
        }
    }
}
