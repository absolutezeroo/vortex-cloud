using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPetCommandBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "energy_cost",
                table: "pet_commands",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder
                .AddColumn<string>(
                    name: "posture",
                    table: "pet_commands",
                    type: "varchar(512)",
                    maxLength: 512,
                    nullable: false,
                    defaultValue: ""
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "xp_reward",
                table: "pet_commands",
                type: "int",
                nullable: false,
                defaultValue: 0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "energy_cost", table: "pet_commands");

            migrationBuilder.DropColumn(name: "posture", table: "pet_commands");

            migrationBuilder.DropColumn(name: "xp_reward", table: "pet_commands");
        }
    }
}
