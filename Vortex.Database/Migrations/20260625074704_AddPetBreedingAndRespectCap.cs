using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPetBreedingAndRespectCap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "can_breed",
                table: "pets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<int>(
                name: "parent_one_id",
                table: "pets",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "parent_two_id",
                table: "pets",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<DateOnly>(
                name: "respect_last_reset_date",
                table: "pets",
                type: "date",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "respect_today_count",
                table: "pets",
                type: "int",
                nullable: false,
                defaultValue: 0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "can_breed", table: "pets");

            migrationBuilder.DropColumn(name: "parent_one_id", table: "pets");

            migrationBuilder.DropColumn(name: "parent_two_id", table: "pets");

            migrationBuilder.DropColumn(name: "respect_last_reset_date", table: "pets");

            migrationBuilder.DropColumn(name: "respect_today_count", table: "pets");
        }
    }
}
