using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Repairs the two furniture families the asset-binding pass left behind: 348 crackables that
    /// counted no hits, and 12 scoreboards the client never recognised as scoreboards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both were invisible to the earlier pass for reasons worth keeping. The crackables because
    /// <c>furni_stuff_data_types.sql</c> deliberately left the family out — a crackable needs a
    /// prize-pool binding as well as a format, so flipping the format alone would have produced
    /// half-configured furniture — and the five that were curated by hand were matched on their
    /// name containing "crackable", which most crackables' names do not. The scoreboards because
    /// asset classnames are matched exactly and these rows carry a <c>*variant</c> suffix
    /// (<c>highscore_classic*1</c>) that no asset name has.
    /// </para>
    /// <para>
    /// The hit count the crackables get is a stated default rather than a derived value: nothing in
    /// furnidata, the assets or <c>total_states</c> carries it. The seed says so at the statement,
    /// and the dashboard's Prize Pools page is where it is meant to be retuned.
    /// </para>
    /// </remarks>
    public partial class FixCrackableAndHighscoreFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The starter pool this joins on is seeded by AddPrizePoolBindings, several migrations
            // back; the insert below simply matches nothing on a hotel that never had it.
            migrationBuilder.Sql(SeedScripts.Read("crackable_and_highscore_families.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to go back to. The previous values named logic no side binds — reverting would
            // restore 360 definitions to being inert, which is the bug rather than a state.
        }
    }
}
