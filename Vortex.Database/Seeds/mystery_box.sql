-- Mystery box reference data.
--
-- Every statement does nothing when the referenced furniture definition is absent (a hotel may ship
-- a trimmed furnidata), and re-running it is harmless.

-- The client only offers the "use" context menu, the wait dialog and the inscription dialog when the
-- furniture carries the matching logic name (RoomObjectLogicEnum: furniture_mysterybox /
-- furniture_mysterytrophy). Shipped definitions default to "default", which renders them inert.
UPDATE furniture_definitions SET logic = 'furniture_mysterybox'
    WHERE name = 'mystery_box' AND logic <> 'furniture_mysterybox';

UPDATE furniture_definitions SET logic = 'furniture_mysterytrophy'
    WHERE name = 'gift_mysterytrophy' AND logic <> 'furniture_mysterytrophy';

-- The box's colour is its furniture state, not a recolour: mystery_box.nitro ships 25 states —
-- state 0 colourless, then eight groups of three, one per colour, in KEY_COLORS order. A shipped
-- total_states of 2 would leave the coloured states unreachable to anything that clamps on it.
UPDATE furniture_definitions SET total_states = 25
    WHERE name = 'mystery_box' AND total_states < 25;

-- Box pool: any colour, floor furni, equal odds. Nothing here is a rare — the point is that the pool
-- is non-empty out of the box; an operator retunes it with plain UPDATEs.
INSERT INTO mystery_box_prizes (pool, color, product_type, furniture_definition_id, extra_param, weight, enabled)
    SELECT 0, '', 0, d.id, '', 10, 1 FROM furniture_definitions d
    WHERE d.name IN ('present_gen1', 'chair_norja', 'table_norja_med', 'sofa_silo', 'shelves_norja')
      AND NOT EXISTS (
          SELECT 1 FROM (SELECT * FROM mystery_box_prizes) p
          WHERE p.pool = 0 AND p.furniture_definition_id = d.id
      );

-- Trophy pool: the world cup trophies the furnidata description promises ("gold silver bronze"),
-- weighted so bronze is the common outcome.
INSERT INTO mystery_box_prizes (pool, color, product_type, furniture_definition_id, extra_param, weight, enabled)
    SELECT 1, '', 0, d.id, '', w.weight, 1 FROM furniture_definitions d
    JOIN (
        SELECT 'fball_trophybrasil_gold' AS name, 1 AS weight
        UNION ALL SELECT 'fball_trophybrasil_silver', 3
        UNION ALL SELECT 'fball_trophybrasil_bronze', 6
    ) w ON w.name = d.name
    WHERE NOT EXISTS (
        SELECT 1 FROM (SELECT * FROM mystery_box_prizes) p
        WHERE p.pool = 1 AND p.furniture_definition_id = d.id
    );
