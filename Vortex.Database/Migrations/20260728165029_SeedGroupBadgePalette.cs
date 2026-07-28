using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Applies the guild badge-part and colour palettes. Both tables shipped empty because the seed
    /// scripts under <c>Vortex.Database/Seeds</c> were referenced by nothing, which left the badge
    /// editor with no shapes or symbols to offer and made colour resolution fall back to white.
    /// </summary>
    public partial class SeedGroupBadgePalette : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SeedScripts.Read("group_badge_parts.sql"));
            migrationBuilder.Sql(SeedScripts.Read("group_colors.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reference data only — clearing the palettes would break every badge already designed
            // against these part/colour ids, so the rollback deliberately leaves them in place.
        }
    }
}
