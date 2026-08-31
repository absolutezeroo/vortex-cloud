using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Fills the fishing content tables the previous migration created: three zones, twelve species,
    /// a level curve and five rod tiers.
    /// </summary>
    /// <remarks>
    /// Without this the tables ship empty, and an empty zone table means every click on a spot is
    /// answered <c>NotASpot</c> — the feature would be present and inert.
    ///
    /// <para>Content only. The tunables are admin-editable gameplay config and live in
    /// <c>IServerConfigGrain</c> under the <c>fishing.*</c> keys (see
    /// <c>Vortex.Fishing/FishingConfig.cs</c>), where the compiled defaults apply until an operator
    /// overrides one — so there is nothing to seed for them.</para>
    ///
    /// <para>The seed is <c>INSERT IGNORE</c> throughout, so it can be applied to a hotel that has
    /// already tuned these numbers without overwriting anything. Every value in it is a guess: see
    /// the header of <c>Seeds/fishing.sql</c> and the client's
    /// <c>docs/vortex-original/fishing.md</c>.</para>
    /// </remarks>
    public partial class SeedFishingDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(SeedScripts.Read("fishing.sql"));

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reference data. Players' records and derby entries point at these species and zone
            // ids, so clearing the definitions would leave a Fishopedia naming rows that no longer
            // exist — the rollback deliberately leaves them in place.
        }
    }
}
