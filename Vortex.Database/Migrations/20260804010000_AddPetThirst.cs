using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Splits thirst out of energy, so pets carry the four needs Habbo counts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Thirst was read off the energy bar. One number therefore answered both "wants a drink" and
    /// "wants a nap": a pet that drank stopped being sleepy, and a pet that slept stopped being
    /// thirsty. Habbo keeps hunger, thirst, energy and happiness apart, and the info panel's own
    /// description lists them separately.
    /// </para>
    /// <para>
    /// Existing pets start slaked rather than parched -- a pet that has been fine all along should
    /// not bolt for the water bowl the moment the column appears. The food table gains its own
    /// thirst column so a bowl of water can fill thirst and nothing else, while food gives back a
    /// little energy, which is what lets a fed pet be trained again.
    /// </para>
    /// </remarks>
    public partial class AddPetThirst : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "thirst",
                table: "pets",
                type: "int",
                nullable: false,
                defaultValue: 100
            );

            migrationBuilder.AddColumn<int>(
                name: "thirst",
                table: "pet_food",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.Sql(SeedScripts.Read("pet_food.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "thirst", table: "pets");
            migrationBuilder.DropColumn(name: "thirst", table: "pet_food");
        }
    }
}
