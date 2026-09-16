using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Re-applies <c>furni_logic_self_named_rebind.sql</c>, which has gained a name:
    /// <c>wf_xtra_scan_chest_furni_by_type</c>, the chest scanner add-on, now that a logic answers
    /// to it.
    /// </summary>
    /// <remarks>
    /// The seed is the list of classnames the generated binding pass rebinds away from their own
    /// behaviour, and it grows every time this emulator implements one of them. A hotel that already
    /// ran <see cref="RebindSelfNamedFurnitureLogic"/> would never revisit the file, so each
    /// addition needs its own migration — the statement is scoped by <c>logic &lt;&gt; name</c> and
    /// touches nothing it has already fixed.
    /// </remarks>
    public partial class RebindChestScannerLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(SeedScripts.Read("furni_logic_self_named_rebind.sql"));

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The value being replaced is the one that made the furni inert.
        }
    }
}
