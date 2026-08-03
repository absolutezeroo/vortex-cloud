using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Binds the catalogue to real room-object logic. 53 782 of 55 279 definitions carried a
    /// <c>logic</c> string that matches nothing registered, so they resolved to the family default
    /// and silently did nothing: vending machines, beds, teleports, pressure plates, switches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Most of that number is harmless — 47 320 rows say <c>none</c>, and a plain furni genuinely is
    /// the default logic. The damage is the ~6 400 rows naming behaviour the server then never ran.
    /// </para>
    /// <para>
    /// The values come from the shipped assets (each <c>.nitro</c> declares the <c>logicType</c> the
    /// client binds), not from the Arcturus dump, which has no logic column at all — only an
    /// <c>interaction_type</c> the converter used to copy verbatim.
    /// </para>
    /// <para>
    /// A definition already bound to a registered Vortex logic is left untouched. The client's names
    /// are not a superset of ours: it calls a gate <c>furniture_multistate</c> because Flash derives
    /// blocking from the visualization, while Vortex resolves walkability server-side. Taking the
    /// asset value unconditionally would have overwritten 744 working definitions — 375 gates, which
    /// would have stopped blocking, plus rollers and wired furni.
    /// </para>
    /// <para>
    /// This does not make every furni work. 4 016 definitions across 64 families name behaviour
    /// Vortex has not implemented; they now resolve to the correct family default and log which
    /// logic is missing, which is a better starting point than the silence they had before.
    /// </para>
    /// </remarks>
    public partial class BindFurnitureLogicFromAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SeedScripts.Read("furni_logic_bindings.sql"));

            // Keyed on the logic values the statement above just wrote, so the order is load-bearing.
            migrationBuilder.Sql(SeedScripts.Read("furni_stuff_data_types.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The previous values bound to nothing — reverting would restore 53 782 definitions to
            // resolving against a logic that does not exist. There is no working state to go back to.
        }
    }
}
