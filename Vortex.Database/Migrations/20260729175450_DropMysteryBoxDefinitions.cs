using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Drops the box colour registration table. A box's colour is not definition-level data: the
    /// sprite carries no recolour, only 25 states (colourless, then eight groups of three), so the
    /// colour lives in the furniture instance's own state. Keying it off the definition also capped a
    /// hotel at one colour, because furniture_definitions is unique on (sprite_id, type, category)
    /// and every colour would have needed its own row on the same sprite.
    /// </summary>
    public partial class DropMysteryBoxDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "mystery_box_definitions");

            // Boxes handed out before the switch sit at the colourless state 0 and can never pair
            // with a key. Repaint them purple -- the colour the seed registered -- so nothing already
            // in a player's hands is left dead.
            migrationBuilder.Sql(
                """
                UPDATE furniture f
                JOIN furniture_definitions d ON d.id = f.definition_id
                SET f.extra_data = '{"stuff":{"data":"1"}}'
                WHERE d.logic = 'furniture_mysterybox'
                  AND f.deleted_at IS NULL
                  AND (f.extra_data IS NULL OR f.extra_data NOT LIKE '%"data"%');
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "mystery_box_definitions",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        furniture_definition_id = table.Column<int>(type: "int", nullable: false),
                        color = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: true
                        ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_mystery_box_definitions", x => x.id);
                        table.ForeignKey(
                            name: "FK_mystery_box_definitions_furniture_definitions_furniture_defi~",
                            column: x => x.furniture_definition_id,
                            principalTable: "furniture_definitions",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_mystery_box_definitions_color",
                table: "mystery_box_definitions",
                column: "color"
            );

            migrationBuilder.CreateIndex(
                name: "IX_mystery_box_definitions_furniture_definition_id",
                table: "mystery_box_definitions",
                column: "furniture_definition_id",
                unique: true
            );
        }
    }
}
