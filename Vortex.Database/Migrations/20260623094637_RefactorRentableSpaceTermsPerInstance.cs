using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class RefactorRentableSpaceTermsPerInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rentable_space_terms_currency_types_currency_type_id",
                table: "rentable_space_terms"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_rentable_space_terms_furniture_definitions_furniture_definit~",
                table: "rentable_space_terms"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_rentable_space_terms",
                table: "rentable_space_terms"
            );

            migrationBuilder.RenameTable(
                name: "rentable_space_terms",
                newName: "room_rentable_space_terms"
            );

            migrationBuilder.RenameColumn(
                name: "furniture_definition_id",
                table: "room_rentable_space_terms",
                newName: "furniture_id"
            );

            migrationBuilder.RenameIndex(
                name: "IX_rentable_space_terms_furniture_definition_id",
                table: "room_rentable_space_terms",
                newName: "IX_room_rentable_space_terms_furniture_id"
            );

            migrationBuilder.RenameIndex(
                name: "IX_rentable_space_terms_currency_type_id",
                table: "room_rentable_space_terms",
                newName: "IX_room_rentable_space_terms_currency_type_id"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_room_rentable_space_terms",
                table: "room_rentable_space_terms",
                column: "id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_room_rentable_space_terms_currency_types_currency_type_id",
                table: "room_rentable_space_terms",
                column: "currency_type_id",
                principalTable: "currency_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_room_rentable_space_terms_furniture_furniture_id",
                table: "room_rentable_space_terms",
                column: "furniture_id",
                principalTable: "furniture",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_room_rentable_space_terms_currency_types_currency_type_id",
                table: "room_rentable_space_terms"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_room_rentable_space_terms_furniture_furniture_id",
                table: "room_rentable_space_terms"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_room_rentable_space_terms",
                table: "room_rentable_space_terms"
            );

            migrationBuilder.RenameTable(
                name: "room_rentable_space_terms",
                newName: "rentable_space_terms"
            );

            migrationBuilder.RenameColumn(
                name: "furniture_id",
                table: "rentable_space_terms",
                newName: "furniture_definition_id"
            );

            migrationBuilder.RenameIndex(
                name: "IX_room_rentable_space_terms_furniture_id",
                table: "rentable_space_terms",
                newName: "IX_rentable_space_terms_furniture_definition_id"
            );

            migrationBuilder.RenameIndex(
                name: "IX_room_rentable_space_terms_currency_type_id",
                table: "rentable_space_terms",
                newName: "IX_rentable_space_terms_currency_type_id"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_rentable_space_terms",
                table: "rentable_space_terms",
                column: "id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_rentable_space_terms_currency_types_currency_type_id",
                table: "rentable_space_terms",
                column: "currency_type_id",
                principalTable: "currency_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_rentable_space_terms_furniture_definitions_furniture_definit~",
                table: "rentable_space_terms",
                column: "furniture_definition_id",
                principalTable: "furniture_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
