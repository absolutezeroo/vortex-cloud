using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Rebuilds the pet food table, which fed the wrong species and starved most of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shipped seed handed each food to exactly one pet type, assigned sequentially in file
    /// order, on a legend that did not match the client: it read <c>8=devil, 10=horse, 16=meow</c>
    /// where the client's <c>pet.type.*</c> keys say 8=Spider, 10=Chicken, 15=Horse,
    /// 16=Monsterplant. Salmon went to spiders, Hay to the Monster, Dragon food to cats, and the
    /// Monsterplant -- which the room excludes from eating entirely -- was given Webbed Grapes. Only
    /// types 0-18 appeared at all, so every bunny, pigeon, baby, dinosaur and cow could never eat
    /// whatever the owner put down.
    /// </para>
    /// <para>
    /// The rebuilt seed grounds each assignment in the furni's own furnidata description wherever it
    /// names a species outright, and in the species' diet otherwise. It also binds the seven food
    /// and drink definitions the catalogue left on <c>default</c>, and the four pet baskets and
    /// blankets, whose previous <c>furniture_pet_nest</c> spelling matched nothing registered.
    /// </para>
    /// </remarks>
    public partial class ReseedPetFoodFromFurnidata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SeedScripts.Read("pet_food.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // There is no working state to go back to: the rows this replaces named the wrong
            // species, and the logic names it repairs bound to nothing at all.
        }
    }
}
