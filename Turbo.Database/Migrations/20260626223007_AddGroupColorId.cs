using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupColorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wipe seed data inserted before color_id column existed (all rows had color_id=0).
            migrationBuilder.Sql("TRUNCATE TABLE `group_colors`;");

            migrationBuilder.AddColumn<int>(
                name: "color_id",
                table: "group_colors",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_colors_color_id",
                table: "group_colors",
                column: "color_id",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_group_colors_color_id", table: "group_colors");

            migrationBuilder.DropColumn(name: "color_id", table: "group_colors");
        }
    }
}
