-- The footballs carry a logic name the client has never heard of.
--
-- `fball_ball*` ships from the reference emulator's items_base as `football`, and the five rows in
-- this database were later hand-edited to `football_ball`. Neither name exists anywhere but here:
-- the furnidata packed in the .nitro binds every one of them to `furniture_pushable`, which is what
-- tools/catalog_converter/output/furniture_definitions.sql already writes, and what the client
-- resolves to its own FurniturePushableLogic.
--
-- The cost of the mismatch is total, in both directions. Server-side the definition matches no
-- registered logic, so the ball falls back to the default floor behaviour: it is not walkable, it
-- reports no walk-on, and FootballGame never hears that anybody kicked it -- the ball does not move
-- at all. Client-side an unknown name falls back to the basic furni logic, which does not
-- interpolate the slide or read the roll state.
--
-- Same shape as scripts/sql/wired_logic_binding_fix.sql: read the dry run, keep the way back, then
-- write.

-- 1. DRY RUN. Expect the five fball_ball rows and nothing else.
SELECT id, name AS classname, logic AS logic_now, 'furniture_pushable' AS logic_after
FROM furniture_definitions
WHERE logic IN ('football', 'football_ball')
  AND name LIKE 'fball_ball%'
ORDER BY name;

-- 2. The way back, into the same table the wired binding fix uses.
CREATE TABLE IF NOT EXISTS wired_logic_binding_backup (
    definition_id INT PRIMARY KEY,
    classname     VARCHAR(255) NOT NULL,
    logic_before  VARCHAR(255) NOT NULL,
    taken_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO wired_logic_binding_backup (definition_id, classname, logic_before)
SELECT id, name, logic
FROM furniture_definitions
WHERE logic IN ('football', 'football_ball')
  AND name LIKE 'fball_ball%'
ON DUPLICATE KEY UPDATE logic_before = VALUES(logic_before);

-- 3. The fix.
UPDATE furniture_definitions
SET logic = 'furniture_pushable'
WHERE logic IN ('football', 'football_ball')
  AND name LIKE 'fball_ball%';

-- 4. Proof. Every football is now on the pushable logic, and no definition is left on a name the
--    server does not register. The two custom balls below are deliberately NOT swept by step 3:
--    they are somebody's uploaded furni, and `Ballon_Adidas` / `Ballons_Puma_Nike` want the same
--    treatment while `irinc_shadow_bb1_cannon` -- a Banzai cannon wearing the same wrong logic --
--    does not. Decide those by hand.
SELECT id, name AS classname, logic
FROM furniture_definitions
WHERE name LIKE 'fball_ball%' OR logic IN ('football', 'football_ball')
ORDER BY logic, name;
