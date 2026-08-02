-- Crackable furniture reference data.
--
-- Every statement does nothing when the referenced furniture definition is absent (a hotel may ship
-- a trimmed furnidata), and re-running it is harmless.

-- A starter pool so the seeded crackables pay out something on a fresh hotel. Nothing here is a
-- rare: the point is that the pool is non-empty, and an operator retunes it from the dashboard.
INSERT INTO prize_pools (code, name, variants, enabled, notes)
    SELECT 'crackable-starter', 'Crackable starter', '', 1,
           'Default pool for the seeded crackable furniture. Retune or point the bindings elsewhere.'
    WHERE NOT EXISTS (
        SELECT 1 FROM (SELECT * FROM prize_pools) p WHERE p.code = 'crackable-starter'
    );

INSERT INTO prize_pool_entries (pool_id, variant, product_type, furniture_definition_id, extra_param, weight, enabled)
    SELECT pool.id, '', 0, d.id, '', w.weight, 1
    FROM furniture_definitions d
    JOIN prize_pools pool ON pool.code = 'crackable-starter'
    JOIN (
        SELECT 'present_gen1' AS name, 10 AS weight
        UNION ALL SELECT 'chair_norja', 10
        UNION ALL SELECT 'sofa_silo', 6
        UNION ALL SELECT 'shelves_norja', 6
    ) w ON w.name = d.name
    WHERE NOT EXISTS (
        SELECT 1 FROM (SELECT * FROM prize_pool_entries) e
        WHERE e.pool_id = pool.id AND e.furniture_definition_id = d.id
    );

-- The client only accumulates hits and shows "hits remaining" on furniture carrying the crackable
-- logic (RoomObjectLogicEnum: furniture_crackable) whose data arrives in format 7. Shipped
-- definitions default to "default" and the legacy format, which renders them inert.
UPDATE furniture_definitions SET logic = 'furniture_crackable', stuff_data_type = 7
    WHERE name IN (
        'easter_c23_craftbotcrackable',
        'circus_c24_bluetentcrackable',
        'circus_c24_redtentcrackable',
        'wonderland_c25_crackableb',
        'wonderland_c25_redcrackableb'
    )
-- Guarded on the outcome, not on the logic column alone. Testing only `logic <> 'furniture_crackable'`
-- skips a definition that already carries the logic but was left on another stuff data format, and
-- that row can then never be repaired by re-running the seed -- which is exactly how a crackable ends
-- up running the crackable logic with no counters to write to.
      AND (logic <> 'furniture_crackable' OR stuff_data_type <> 7);

-- How many hits each takes. Nothing in furnidata carries this -- the client only renders the
-- counters the server sends -- so it lives with the binding.
INSERT INTO prize_pool_bindings (furniture_definition_id, pool_id, hits_required, enabled)
    SELECT d.id, pool.id, h.hits, 1
    FROM furniture_definitions d
    JOIN prize_pools pool ON pool.code = 'crackable-starter'
    JOIN (
        SELECT 'easter_c23_craftbotcrackable' AS name, 6 AS hits
        UNION ALL SELECT 'circus_c24_bluetentcrackable', 10
        UNION ALL SELECT 'circus_c24_redtentcrackable', 10
        UNION ALL SELECT 'wonderland_c25_crackableb', 8
        UNION ALL SELECT 'wonderland_c25_redcrackableb', 8
    ) h ON h.name = d.name
    WHERE NOT EXISTS (
        SELECT 1 FROM (SELECT * FROM prize_pool_bindings) b
        WHERE b.furniture_definition_id = d.id
    );
