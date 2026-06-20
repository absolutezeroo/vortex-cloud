using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRentableSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "rentable_space_furniture_id",
                table: "furniture",
                type: "int",
                nullable: true
            );

            migrationBuilder
                .CreateTable(
                    name: "rentable_space_terms",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        furniture_definition_id = table.Column<int>(type: "int", nullable: false),
                        price = table.Column<int>(type: "int", nullable: false),
                        currency_type_id = table.Column<int>(type: "int", nullable: false),
                        rent_duration_seconds = table.Column<int>(type: "int", nullable: false),
                        requires_hc = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                        table.PrimaryKey("PK_rentable_space_terms", x => x.id);
                        table.ForeignKey(
                            name: "FK_rentable_space_terms_currency_types_currency_type_id",
                            column: x => x.currency_type_id,
                            principalTable: "currency_types",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_rentable_space_terms_furniture_definitions_furniture_definit~",
                            column: x => x.furniture_definition_id,
                            principalTable: "furniture_definitions",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "room_rentable_spaces",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        furniture_id = table.Column<int>(type: "int", nullable: false),
                        renter_player_id = table.Column<int>(type: "int", nullable: true),
                        rented_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                        table.PrimaryKey("PK_room_rentable_spaces", x => x.id);
                        table.ForeignKey(
                            name: "FK_room_rentable_spaces_furniture_furniture_id",
                            column: x => x.furniture_id,
                            principalTable: "furniture",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_room_rentable_spaces_players_renter_player_id",
                            column: x => x.renter_player_id,
                            principalTable: "players",
                            principalColumn: "id"
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_furniture_rentable_space_furniture_id",
                table: "furniture",
                column: "rentable_space_furniture_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_rentable_space_terms_currency_type_id",
                table: "rentable_space_terms",
                column: "currency_type_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_rentable_space_terms_furniture_definition_id",
                table: "rentable_space_terms",
                column: "furniture_definition_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_room_rentable_spaces_furniture_id",
                table: "room_rentable_spaces",
                column: "furniture_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_room_rentable_spaces_renter_player_id",
                table: "room_rentable_spaces",
                column: "renter_player_id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_furniture_furniture_rentable_space_furniture_id",
                table: "furniture",
                column: "rentable_space_furniture_id",
                principalTable: "furniture",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_furniture_furniture_rentable_space_furniture_id",
                table: "furniture"
            );

            migrationBuilder.DropTable(name: "rentable_space_terms");

            migrationBuilder.DropTable(name: "room_rentable_spaces");

            migrationBuilder.DropIndex(
                name: "IX_furniture_rentable_space_furniture_id",
                table: "furniture"
            );

            migrationBuilder.DropColumn(name: "rentable_space_furniture_id", table: "furniture");
        }
    }
}
