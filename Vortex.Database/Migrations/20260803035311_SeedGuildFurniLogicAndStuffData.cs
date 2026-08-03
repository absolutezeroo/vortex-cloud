using Microsoft.EntityFrameworkCore.Migrations;
using Vortex.Database.Seeds;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Repairs guild furniture, which rendered white in-game because none of it ever carried the
    /// guild's identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two shipped defaults broke the chain. <c>logic</c> held the raw Arcturus
    /// <c>interaction_type</c> (<c>guild_furni</c>, <c>guild_gate</c>) or <c>none</c>, none of which
    /// is a registered Vortex room-object logic. And <c>stuff_data_type</c> held 0 (LegacyKey) while
    /// the client reads a string array, which made <c>InventoryGrain.GrantCatalogOfferAsync</c> skip
    /// stamping the guild id, badge and recolours altogether — it gates on
    /// <c>StuffDataType.StringKey</c>. The client then fell back to its own defaults, which is the
    /// white the players were seeing.
    /// </para>
    /// <para>
    /// Order matters. The backfill below keys off the *old* <c>logic</c> values, so it has to run
    /// before <c>guild_furni_logic.sql</c> overwrites them.
    /// </para>
    /// </remarks>
    public partial class SeedGuildFurniLogicAndStuffData : Migration
    {
        /// <summary>
        /// Reconstructs the guild stuff data for furniture bought before the stamping worked.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Shape mirrors <c>InventoryGrain.BuildGuildExtraData</c> exactly: the <c>stuff</c> section
        /// of the extra-data blob, holding the five-element array of
        /// <c>Vortex.Primitives.Groups.GuildFurniStuffData</c> — state, guild id, badge code, then
        /// both recolours as bare RGB hex. Colour resolution mirrors
        /// <c>GroupBadgePartProvider.ResolveColorHex</c>: the palette entry for the stored id, else
        /// the first palette entry, else white.
        /// </para>
        /// <para>
        /// Only genuine guild furni is touched. The custom packs that reuse the same client logic to
        /// be freely recolourable (<c>habbox_clr_*</c>, <c>shadow_clr_*</c>, <c>hlive_clr*</c>,
        /// <c>recolor_*</c>) are deliberately left alone: they are not bound to a guild, and
        /// stamping them with whichever guild their owner happens to belong to would invent a
        /// relationship that never existed. They still get the corrected stuff-data format, so a
        /// colour picker can fill them in later.
        /// </para>
        /// <para>
        /// The guild is only inferred where it is unambiguous — the room the item stands in is a
        /// guild base, or the owner belongs to exactly one guild. Anything else keeps its empty
        /// stuff data rather than guessing; those items render exactly as they do today and can be
        /// re-stamped by hand.
        /// </para>
        /// </remarks>
        private const string BackfillGuildExtraData = """
            UPDATE `furniture` f
              JOIN `furniture_definitions` d
                ON d.id = f.definition_id
               AND d.logic IN ('guild_furni', 'guild_gate')
              JOIN `groups` g
                ON g.deleted_at IS NULL
               AND g.id = COALESCE(
                     (SELECT b.id
                        FROM `groups` b
                       WHERE b.deleted_at IS NULL
                         AND b.room_id = f.room_id
                       ORDER BY b.id
                       LIMIT 1),
                     (SELECT s.group_id
                        FROM (SELECT gm.player_id, MIN(gm.group_id) AS group_id
                                FROM `group_members` gm
                               WHERE gm.deleted_at IS NULL
                               GROUP BY gm.player_id
                              HAVING COUNT(DISTINCT gm.group_id) = 1) s
                       WHERE s.player_id = f.player_id))
               SET f.extra_data = CONCAT(
                     '{"stuff":{"data":["0","', g.id,
                     '","', REPLACE(g.badge, '"', ''),
                     '","', COALESCE(
                              (SELECT c.color_hex FROM `group_colors` c WHERE c.color_id = g.color_one),
                              (SELECT c.color_hex FROM `group_colors` c ORDER BY c.color_id LIMIT 1),
                              'ffffff'),
                     '","', COALESCE(
                              (SELECT c.color_hex FROM `group_colors` c WHERE c.color_id = g.color_two),
                              (SELECT c.color_hex FROM `group_colors` c ORDER BY c.color_id LIMIT 1),
                              'ffffff'),
                     '"]}}')
             WHERE f.deleted_at IS NULL
               AND (f.extra_data IS NULL OR f.extra_data IN ('', '{}'));
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BackfillGuildExtraData);
            migrationBuilder.Sql(SeedScripts.Read("guild_furni_logic.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting the logic names and the stuff-data format would put the furni back to
            // rendering white, and the original `logic` strings bound to nothing in the first place,
            // so there is no working state to return to. The backfilled stuff data is left in place
            // for the same reason: it is the correct data, and dropping it would lose the guild
            // identity of every item bought before this migration.
        }
    }
}
