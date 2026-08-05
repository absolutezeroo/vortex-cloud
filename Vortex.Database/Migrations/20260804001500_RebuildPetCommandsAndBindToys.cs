using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Puts pet commands on the client's own numbering, and gives the toys a logic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>pet_commands</c> numbered its rows 0=Sit, 1=Stand, 2=Lay Down while
    /// <c>pet_command_names</c> is lifted from the client's text bundle, where 0=Free, 1=Sit,
    /// 8=Stand. Both are read with the same id, so telling a pet to Sit made it stand, Free made it
    /// sit, and Nest matched no row and was ignored. Two postures were invented as well -- `rll` and
    /// `flp` are not ids any pet asset declares, so those tricks resolved to standing.
    /// </para>
    /// <para>
    /// The toys have never done anything: all four shipped on <c>default</c>, so no pet could reach
    /// one, in a game whose own guides say toys are what cheer a pet up.
    /// </para>
    /// </remarks>
    public partial class RebuildPetCommandsAndBindToys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SeedScripts.Read("pet_commands.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The rows this replaces answered to the wrong words, and the toys bound to nothing;
            // there is no working state to go back to.
        }
    }
}
