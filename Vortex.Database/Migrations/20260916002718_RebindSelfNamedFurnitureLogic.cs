using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Gives thirty-one definitions their own behaviour back, after the asset-derived binding pass
    /// bound them to the plain floor logic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="BindFurnitureLogicFromAssets"/> applies a generated pass whose statements are
    /// name-scoped. It leaves a definition alone when the value it already carries is a registered
    /// Vortex logic — but it decided that from the dump it read when it was generated, so a
    /// behaviour this emulator gained afterwards was written over, and so was every namesake of a
    /// protected row, a classname not being a key in this database.
    /// </para>
    /// <para>
    /// Nothing reported it. <c>furniture_basic</c> and <c>furniture_multistate</c> resolve perfectly
    /// well; the furni places, sits there and does nothing. <c>room_invisible_click_tile</c> is what
    /// made it visible: bound to <c>furniture_basic</c> it never marks its tile as a click listener,
    /// so the room publishes no tile click and <c>wf_trg_click_tile</c> — implemented, registered
    /// and reachable — can never fire.
    /// </para>
    /// <para>
    /// The same repair was made by hand twice before, in <c>scripts/sql/wired_logic_binding_fix.sql</c>
    /// and <c>scripts/sql/wired_contract_logic_binding.sql</c>. Those cover the wired boxes on hotels
    /// where somebody ran them; this covers every install, including the fresh ones the generated
    /// pass breaks on first boot.
    /// </para>
    /// </remarks>
    public partial class RebindSelfNamedFurnitureLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(SeedScripts.Read("furni_logic_self_named_rebind.sql"));

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The value being replaced is the one that made the furni inert. There is no working
            // state to go back to.
        }
    }
}
