using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixPetFoodCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pet_food_furniture_definitions_furniture_definition_id",
                table: "pet_food"
            );

            migrationBuilder.DropIndex(
                name: "IX_pet_food_furniture_definition_id",
                table: "pet_food"
            );

            migrationBuilder.DropIndex(name: "IX_pet_food_pet_type", table: "pet_food");

            migrationBuilder.CreateIndex(
                name: "IX_pet_food_furniture_definition_id_pet_type",
                table: "pet_food",
                columns: new[] { "furniture_definition_id", "pet_type" },
                unique: true
            );

            migrationBuilder.AddForeignKey(
                name: "FK_pet_food_furniture_definitions_furniture_definition_id",
                table: "pet_food",
                column: "furniture_definition_id",
                principalTable: "furniture_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pet_food_furniture_definitions_furniture_definition_id",
                table: "pet_food"
            );

            migrationBuilder.DropIndex(
                name: "IX_pet_food_furniture_definition_id_pet_type",
                table: "pet_food"
            );

            migrationBuilder.CreateIndex(
                name: "IX_pet_food_furniture_definition_id",
                table: "pet_food",
                column: "furniture_definition_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_pet_food_pet_type",
                table: "pet_food",
                column: "pet_type"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_pet_food_furniture_definitions_furniture_definition_id",
                table: "pet_food",
                column: "furniture_definition_id",
                principalTable: "furniture_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
