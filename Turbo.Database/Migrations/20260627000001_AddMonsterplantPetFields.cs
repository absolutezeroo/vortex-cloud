using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterplantPetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "rarity_level",
                table: "pets",
                type: "int",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "last_watered_at",
                table: "pets",
                type: "datetime(6)",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "last_watered_at", table: "pets");

            migrationBuilder.DropColumn(name: "rarity_level", table: "pets");
        }
    }
}
