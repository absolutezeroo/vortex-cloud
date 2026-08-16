using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class RoomDecorationIdsAsStrings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .AlterColumn<string>(
                    name: "paint_wall",
                    table: "rooms",
                    type: "varchar(512)",
                    maxLength: 512,
                    nullable: true,
                    oldClrType: typeof(double),
                    oldType: "double",
                    oldDefaultValue: 0.0
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "paint_landscape",
                    table: "rooms",
                    type: "varchar(512)",
                    maxLength: 512,
                    nullable: true,
                    oldClrType: typeof(double),
                    oldType: "double",
                    oldDefaultValue: 0.0
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "paint_floor",
                    table: "rooms",
                    type: "varchar(512)",
                    maxLength: 512,
                    nullable: true,
                    oldClrType: typeof(double),
                    oldType: "double",
                    oldDefaultValue: 0.0
                )
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .AlterColumn<double>(
                    name: "paint_wall",
                    table: "rooms",
                    type: "double",
                    nullable: false,
                    defaultValue: 0.0,
                    oldClrType: typeof(string),
                    oldType: "varchar(512)",
                    oldMaxLength: 512,
                    oldNullable: true
                )
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<double>(
                    name: "paint_landscape",
                    table: "rooms",
                    type: "double",
                    nullable: false,
                    defaultValue: 0.0,
                    oldClrType: typeof(string),
                    oldType: "varchar(512)",
                    oldMaxLength: 512,
                    oldNullable: true
                )
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<double>(
                    name: "paint_floor",
                    table: "rooms",
                    type: "double",
                    nullable: false,
                    defaultValue: 0.0,
                    oldClrType: typeof(string),
                    oldType: "varchar(512)",
                    oldMaxLength: 512,
                    oldNullable: true
                )
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
