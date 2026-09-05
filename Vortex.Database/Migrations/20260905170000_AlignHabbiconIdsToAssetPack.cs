using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Renumbers the seeded Habbicons and their collections onto the ids the client's asset pack
    /// uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>habbicons.sql</c> shipped ids 1..33 and said so: the client resolves a Habbicon's artwork
    /// by <b>id</b> from its own <c>habbicons.json</c>, which we did not have, and the header warned
    /// that an operator installing a real pack would have to align them. A pack landed on
    /// 2026-09-05 numbering the same 33 codes 28..60, and its <c>collectionIcons</c> are 5, 6 and 7 —
    /// a duck, a duck with a green scarf (<c>duck2</c>, which we do not seed) and Frank — so
    /// <c>duck</c> is 5 and <c>frank</c> is 7.
    /// </para>
    /// <para>
    /// Not cosmetic: the two numberings <b>overlap</b> at 28..33 with different meanings, so
    /// <c>frank_silly</c> (28, ours) drew the pack's <c>duck_duck</c>. Wrong pictures, not missing
    /// ones — which is the failure that hides, because nothing errors.
    /// </para>
    /// <para>
    /// Keyed on <c>code</c>, never on the old id: codes are the stable identity (they come from the
    /// client's <c>external_flash_texts</c> and the pack names the same ones), and a hotel that
    /// renumbered by hand is still matched. The map is built with <c>WHERE id &lt;&gt; new_id</c>, so a
    /// hotel already aligned produces an empty map and every statement is a no-op — the migration is
    /// re-runnable and safe on a fresh install where the seed already wrote the new ids.
    /// </para>
    /// <para>
    /// The park-then-unpark at +100000 is required, not defensive: old and new ranges overlap, so a
    /// direct <c>UPDATE</c> would collide on the primary key mid-statement. Foreign key checks are
    /// off across that window because <c>habbicons.collection_id</c> and
    /// <c>player_habbicons.habbicon_id</c> are <c>ON DELETE CASCADE</c> only — MySQL restricts
    /// updates, so neither parent nor child can move first.
    /// </para>
    /// <para>
    /// Everything that stores a Habbicon id moves with it: <c>player_habbicons.habbicon_id</c> (so
    /// nobody loses what they own), <c>messenger_messages.habbicon_id</c> (a plain int, 0 meaning
    /// none, so 0 is excluded), and <c>reward_track_prize_rewards.reward_type_id</c> where
    /// <c>kind = 12</c> — the four Introduction Track milestones that hand over a Habbicon.
    /// </para>
    /// </remarks>
    public partial class AlignHabbiconIdsToAssetPack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(Remap(reverse: false));

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(Remap(reverse: true));

        /// <summary>
        /// The whole remap as one script. <c>reverse</c> swaps which column of the code table is the
        /// target, which is the only difference between applying and undoing it.
        /// </summary>
        private static string Remap(bool reverse)
        {
            // code, id under our original seed, id in the client's asset pack.
            (string Code, int Ours, int Pack)[] habbicons =
            [
                ("duck_angel", 1, 36),
                ("duck_cool", 2, 39),
                ("duck_devil", 3, 37),
                ("duck_devious", 4, 43),
                ("duck_duck", 5, 28),
                ("duck_grimace", 6, 42),
                ("duck_happy", 7, 29),
                ("duck_laughing", 8, 41),
                ("duck_love", 9, 49),
                ("duck_metal", 10, 44),
                ("duck_nohear", 11, 33),
                ("duck_nosay", 12, 35),
                ("duck_nosee", 13, 34),
                ("duck_party", 14, 48),
                ("duck_pleading", 15, 45),
                ("duck_pleased", 16, 40),
                ("duck_sad", 17, 30),
                ("duck_shock", 18, 31),
                ("duck_silly", 19, 46),
                ("duck_think", 20, 32),
                ("duck_wink", 21, 47),
                ("duck_spinning", 22, 38),
                ("frank_frank", 23, 50),
                ("frank_happy", 24, 52),
                ("frank_relief", 25, 58),
                ("frank_sad", 26, 53),
                ("frank_scared", 27, 54),
                ("frank_silly", 28, 57),
                ("frank_smile", 29, 51),
                ("frank_stareyes", 30, 60),
                ("frank_surprised", 31, 55),
                ("frank_thinking", 32, 56),
                ("frank_wink", 33, 59),
            ];

            (string Code, int Ours, int Pack)[] collections = [("duck", 1, 5), ("frank", 2, 7)];

            string habbiconRows = string.Join(
                "\n    UNION ALL ",
                habbicons.Select(h =>
                    $"SELECT '{h.Code}' AS code, {(reverse ? h.Ours : h.Pack)} AS new_id"
                )
            );

            string collectionRows = string.Join(
                "\n    UNION ALL ",
                collections.Select(c =>
                    $"SELECT '{c.Code}' AS code, {(reverse ? c.Ours : c.Pack)} AS new_id"
                )
            );

            return $"""
                CREATE TEMPORARY TABLE _vortex_habbicon_remap (
                    old_id INT NOT NULL PRIMARY KEY,
                    new_id INT NOT NULL
                );

                INSERT INTO _vortex_habbicon_remap (old_id, new_id)
                SELECT h.id, m.new_id
                FROM habbicons h
                JOIN ({habbiconRows}) m ON m.code = h.code
                WHERE h.id <> m.new_id;

                CREATE TEMPORARY TABLE _vortex_habbicon_collection_remap (
                    old_id INT NOT NULL PRIMARY KEY,
                    new_id INT NOT NULL
                );

                INSERT INTO _vortex_habbicon_collection_remap (old_id, new_id)
                SELECT c.id, m.new_id
                FROM habbicon_collections c
                JOIN ({collectionRows}) m ON m.code = c.code
                WHERE c.id <> m.new_id;

                SET @vortex_saved_fk_checks := @@FOREIGN_KEY_CHECKS;
                SET FOREIGN_KEY_CHECKS = 0;

                UPDATE habbicons h
                    JOIN _vortex_habbicon_remap r ON h.id = r.old_id
                    SET h.id = r.new_id + 100000;
                UPDATE player_habbicons p
                    JOIN _vortex_habbicon_remap r ON p.habbicon_id = r.old_id
                    SET p.habbicon_id = r.new_id + 100000;
                UPDATE messenger_messages mm
                    JOIN _vortex_habbicon_remap r ON mm.habbicon_id = r.old_id
                    SET mm.habbicon_id = r.new_id + 100000
                    WHERE mm.habbicon_id > 0;

                UPDATE habbicons SET id = id - 100000 WHERE id > 100000;
                UPDATE player_habbicons SET habbicon_id = habbicon_id - 100000 WHERE habbicon_id > 100000;
                UPDATE messenger_messages SET habbicon_id = habbicon_id - 100000 WHERE habbicon_id > 100000;

                UPDATE habbicon_collections c
                    JOIN _vortex_habbicon_collection_remap r ON c.id = r.old_id
                    SET c.id = r.new_id + 100000;
                UPDATE habbicons h
                    JOIN _vortex_habbicon_collection_remap r ON h.collection_id = r.old_id
                    SET h.collection_id = r.new_id + 100000;

                UPDATE habbicon_collections SET id = id - 100000 WHERE id > 100000;
                UPDATE habbicons SET collection_id = collection_id - 100000 WHERE collection_id > 100000;

                UPDATE reward_track_prize_rewards rr
                    JOIN _vortex_habbicon_remap r ON rr.reward_type_id = CAST(r.old_id AS CHAR)
                    SET rr.reward_type_id = CAST(r.new_id AS CHAR)
                    WHERE rr.kind = 12;

                SET FOREIGN_KEY_CHECKS = @vortex_saved_fk_checks;

                DROP TEMPORARY TABLE _vortex_habbicon_remap;
                DROP TEMPORARY TABLE _vortex_habbicon_collection_remap;
                """;
        }
    }
}
