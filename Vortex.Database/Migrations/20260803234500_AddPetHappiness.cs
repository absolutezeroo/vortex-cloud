using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Gives pets the mood stat the info panel has always claimed to show.
    /// </summary>
    /// <remarks>
    /// The panel's happiness bar was fed the pet's nutrition, because nothing else was available:
    /// pets carried hunger and energy and no mood at all. Habbo keeps four stats, and hunger and
    /// thirst never reach this message -- the bar reads happiness and nothing else. Existing pets
    /// start content rather than at zero; a pet that has been fine all along should not look
    /// miserable the moment the column appears.
    /// </remarks>
    public partial class AddPetHappiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "happiness",
                table: "pets",
                type: "int",
                nullable: false,
                defaultValue: 100
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "happiness", table: "pets");
        }
    }
}
