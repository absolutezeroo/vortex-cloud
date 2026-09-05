using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Fills the tables the previous migration created: two Habbicon collections with their
    /// members, and the Introduction Track with its tasks, stages and milestones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Without this both features ship present and inert. An empty <c>habbicons</c> table means the
    /// hub opens on nothing; an empty <c>reward_tracks</c> table means the client is pushed an empty
    /// list and shows no track at all.
    /// </para>
    /// <para>
    /// Content only. Nothing here is code the engine depends on — the Introduction Track is rows,
    /// and the next campaign is different rows written from the dashboard. Each script's header says
    /// which of its values came from the official client and which are ours, and the Habbicon one
    /// says loudly that the <em>ids</em> are a guess an asset pack has to be aligned with.
    /// </para>
    /// <para>
    /// <c>INSERT IGNORE</c> throughout, so applying it to a hotel that has already re-priced a
    /// Habbicon or re-tuned a milestone changes nothing.
    /// </para>
    /// </remarks>
    public partial class SeedHabbiconsAndIntroductionTrack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SeedScripts.Read("habbicons.sql"));
            migrationBuilder.Sql(SeedScripts.Read("reward_track_introduction.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reference data players hold rows against: ownership rows point at these Habbicon ids,
            // and track progress and claims key on 'introduction' and its prize ids. Clearing the
            // definitions would leave an album full of holes and claims naming prizes that no longer
            // exist, so the rollback deliberately leaves them in place.
        }
    }
}
