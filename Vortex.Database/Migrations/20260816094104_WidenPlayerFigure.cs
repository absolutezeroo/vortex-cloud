using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class WidenPlayerFigure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_player_clothing_player_id_figure_set_id",
                table: "player_clothing"
            );

            migrationBuilder
                .AlterColumn<string>(
                    name: "figure",
                    table: "players",
                    type: "varchar(255)",
                    maxLength: 255,
                    nullable: false,
                    defaultValue: "hr-115-42.hd-195-19.ch-3030-82.lg-275-1408.fa-1201.ca-1804-64",
                    oldClrType: typeof(string),
                    oldType: "varchar(100)",
                    oldMaxLength: 100,
                    oldDefaultValue: "hr-115-42.hd-195-19.ch-3030-82.lg-275-1408.fa-1201.ca-1804-64"
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_player_clothing_player_set_product",
                table: "player_clothing",
                columns: new[] { "player_id", "figure_set_id", "product_code" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_player_clothing_player_set_product",
                table: "player_clothing"
            );

            migrationBuilder
                .AlterColumn<string>(
                    name: "figure",
                    table: "players",
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false,
                    defaultValue: "hr-115-42.hd-195-19.ch-3030-82.lg-275-1408.fa-1201.ca-1804-64",
                    oldClrType: typeof(string),
                    oldType: "varchar(255)",
                    oldMaxLength: 255,
                    oldDefaultValue: "hr-115-42.hd-195-19.ch-3030-82.lg-275-1408.fa-1201.ca-1804-64"
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_player_clothing_player_id_figure_set_id",
                table: "player_clothing",
                columns: new[] { "player_id", "figure_set_id" },
                unique: true
            );
        }
    }
}
