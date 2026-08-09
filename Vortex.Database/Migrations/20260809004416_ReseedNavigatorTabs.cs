using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Replaces the navigator rows seeded by <c>SeedNavigatorConfiguration</c>.
    /// <para>
    /// That seed got two things wrong. The tab codes were guessed from a localization key
    /// (<c>official-root</c>) instead of taken from the codes the client actually searches with
    /// (<c>official_view</c>, <c>hotel_view</c>, <c>roomads_view</c>, <c>myworld_view</c>), and the
    /// events tab was missing entirely. It also predates the server answering a tab with one block
    /// per quick link, so the quick-link rows now decide what each tab contains — which is why they
    /// have to be right rather than merely present.
    /// </para>
    /// <para>
    /// The delete is scoped to the exact id ranges the previous seed owns. Nothing else has ever
    /// written those tables, so no operator data is in that range; anything added since keeps its
    /// own ids and survives.
    /// </para>
    /// </summary>
    public partial class ReseedNavigatorTabs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM `navigator_quick_links` WHERE `id` BETWEEN 1 AND 27;"
            );
            migrationBuilder.Sql(
                "DELETE FROM `navigator_top_level_contexts` WHERE `id` BETWEEN 1 AND 4;"
            );

            // Belt and braces: the first seed's `official-root` row could sit outside that range if
            // an operator had already taken id 1.
            migrationBuilder.Sql(
                "DELETE FROM `navigator_top_level_contexts` WHERE `search_code` = 'official-root';"
            );

            migrationBuilder.Sql(SeedScripts.Read("navigator_configuration.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM `navigator_quick_links` WHERE `id` BETWEEN 1 AND 27;"
            );
            migrationBuilder.Sql(
                "DELETE FROM `navigator_top_level_contexts` WHERE `id` BETWEEN 1 AND 4;"
            );
            migrationBuilder.Sql("DELETE FROM `navigator_eventcats` WHERE `id` BETWEEN 1 AND 11;");
        }
    }
}
