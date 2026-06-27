using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupBadgePartId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wipe seed data inserted before part_id column existed (all rows had part_id=0).
            migrationBuilder.Sql("TRUNCATE TABLE `group_badge_parts`;");

            migrationBuilder.DropIndex(
                name: "IX_group_badge_parts_type",
                table: "group_badge_parts"
            );

            migrationBuilder.AddColumn<int>(
                name: "part_id",
                table: "group_badge_parts",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateIndex(
                name: "IX_group_badge_parts_part_id_type",
                table: "group_badge_parts",
                columns: new[] { "part_id", "type" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_group_badge_parts_part_id_type",
                table: "group_badge_parts"
            );

            migrationBuilder.DropColumn(name: "part_id", table: "group_badge_parts");

            migrationBuilder.CreateIndex(
                name: "IX_group_badge_parts_type",
                table: "group_badge_parts",
                column: "type"
            );
        }
    }
}
