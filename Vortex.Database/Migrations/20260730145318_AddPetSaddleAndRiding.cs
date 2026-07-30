using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPetSaddleAndRiding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_saddle",
                table: "pets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "riding_permission",
                table: "pets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "has_saddle", table: "pets");

            migrationBuilder.DropColumn(name: "riding_permission", table: "pets");
        }
    }
}
