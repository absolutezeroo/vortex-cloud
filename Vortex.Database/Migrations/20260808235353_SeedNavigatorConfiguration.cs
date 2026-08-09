using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Seeds the navigator's reference data: three top-level contexts (the client's tabs), their
    /// quick links, and the flat categories a room can be filed under.
    /// <para>
    /// These tables had no seed at all, in any migration or script. On a fresh hotel that meant
    /// <c>NavigatorMetaData</c> shipped zero top-level contexts — the navigator's left pane rendered
    /// empty — and <c>NavigatorProvider.ResolveQueryType</c> had no row for any search code, so every
    /// tab fell through to "all rooms".
    /// </para>
    /// <para>
    /// The script is <c>INSERT IGNORE</c> throughout, so an existing hotel that already configured
    /// its navigator keeps its own rows and this becomes a no-op. <c>Down</c> deliberately removes
    /// only the exact ids this seed inserts.
    /// </para>
    /// </summary>
    public partial class SeedNavigatorConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SeedScripts.Read("navigator_configuration.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM `navigator_quick_links` WHERE `id` BETWEEN 1 AND 16;"
            );
            migrationBuilder.Sql(
                "DELETE FROM `navigator_top_level_contexts` WHERE `id` BETWEEN 1 AND 3;"
            );
            migrationBuilder.Sql("DELETE FROM `navigator_flatcats` WHERE `id` BETWEEN 1 AND 10;");
        }
    }
}
