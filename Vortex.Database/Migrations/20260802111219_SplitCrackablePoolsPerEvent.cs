using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class SplitCrackablePoolsPerEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every seeded crackable pointed at one starter pool, so a Pâques craftbot could drop a
            // circus prize: the model always allowed a pool per furniture, the seed just took a
            // shortcut. Each event gets its own pool, seeded from the starter so none starts empty,
            // and can be retuned independently from the dashboard afterwards.
            migrationBuilder.Sql(
                """
                INSERT INTO prize_pools (code, name, variants, enabled, notes)
                SELECT v.code, v.name, '', 1, v.notes FROM (
                    SELECT 'crackable-easter' AS code, 'Crackable — Easter' AS name,
                           'Easter crackables. Retune freely.' AS notes
                    UNION ALL SELECT 'crackable-circus', 'Crackable — Circus',
                           'Circus tent crackables. Retune freely.'
                    UNION ALL SELECT 'crackable-wonderland', 'Crackable — Wonderland',
                           'Wonderland crackables. Retune freely.'
                ) v
                WHERE NOT EXISTS (SELECT 1 FROM (SELECT code FROM prize_pools) p WHERE p.code = v.code);
                """
            );

            migrationBuilder.Sql(
                """
                INSERT INTO prize_pool_entries
                    (pool_id, variant, product_type, furniture_definition_id, extra_param, weight, enabled)
                SELECT target.id, e.variant, e.product_type, e.furniture_definition_id,
                       e.extra_param, e.weight, e.enabled
                FROM prize_pool_entries e
                JOIN prize_pools source ON source.id = e.pool_id AND source.code = 'crackable-starter'
                JOIN prize_pools target
                  ON target.code IN ('crackable-easter', 'crackable-circus', 'crackable-wonderland')
                WHERE e.deleted_at IS NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM (SELECT pool_id, furniture_definition_id FROM prize_pool_entries) x
                      WHERE x.pool_id = target.id AND x.furniture_definition_id = e.furniture_definition_id
                  );
                """
            );

            // Repoint each binding at its own event's pool, matched on the definition name the
            // crackable seed enabled.
            migrationBuilder.Sql(
                """
                UPDATE prize_pool_bindings b
                JOIN furniture_definitions d ON d.id = b.furniture_definition_id
                JOIN prize_pools target ON target.code = CASE
                        WHEN d.name LIKE 'easter%' THEN 'crackable-easter'
                        WHEN d.name LIKE 'circus%' THEN 'crackable-circus'
                        WHEN d.name LIKE 'wonderland%' THEN 'crackable-wonderland'
                    END
                JOIN prize_pools source ON source.id = b.pool_id AND source.code = 'crackable-starter'
                SET b.pool_id = target.id;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Back onto the single starter pool; the per-event pools and their copies go with it.
            migrationBuilder.Sql(
                """
                UPDATE prize_pool_bindings b
                JOIN prize_pools source ON source.id = b.pool_id
                JOIN prize_pools target ON target.code = 'crackable-starter'
                SET b.pool_id = target.id
                WHERE source.code IN ('crackable-easter', 'crackable-circus', 'crackable-wonderland');
                """
            );

            migrationBuilder.Sql(
                """
                DELETE FROM prize_pools
                WHERE code IN ('crackable-easter', 'crackable-circus', 'crackable-wonderland');
                """
            );
        }
    }
}
