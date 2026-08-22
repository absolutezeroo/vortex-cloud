-- Wired boxes that carry no wired logic at all.
--
-- A room object gets its behaviour from `furniture_definitions.logic`, which RoomObjectModule looks
-- up against the logic classes registered with [RoomObjectLogic("...")]. For a wired box that value
-- is the box's own classname: 192 of them say so already. Sixty do not -- they say
-- `furniture_multistate`, which is the generic "it has states" logic -- so those boxes attach no
-- wired behaviour and do nothing when a trigger reaches them. Nothing reports it: an unresolved
-- logic falls back silently.
--
-- Among the sixty are the five that make a wired chest useful: wf_act_give_currency,
-- wf_act_give_furni, wf_cnd_chest_has_items, wf_cnd_chest_has_item_type and
-- wf_xtra_scan_chest_furni_by_type.
--
-- Run the first two blocks, read them, and only then run the third.

-- 1. DRY RUN -- what would change, and nothing else.
SELECT id, name AS classname, logic AS logic_now, name AS logic_after
FROM furniture_definitions
WHERE name REGEXP '^wf_(act|cnd|trg|xtra)_'
  AND logic = 'furniture_multistate'
ORDER BY name;

-- 2. The way back. Keeps the current value of every row this touches, so the change can be undone
--    even after the fact. Drop the table once you are satisfied.
CREATE TABLE IF NOT EXISTS wired_logic_binding_backup (
    definition_id INT PRIMARY KEY,
    classname     VARCHAR(255) NOT NULL,
    logic_before  VARCHAR(255) NOT NULL,
    taken_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO wired_logic_binding_backup (definition_id, classname, logic_before)
SELECT id, name, logic
FROM furniture_definitions
WHERE name REGEXP '^wf_(act|cnd|trg|xtra)_'
  AND logic = 'furniture_multistate'
ON DUPLICATE KEY UPDATE logic_before = VALUES(logic_before);

-- 3. The fix. Scoped to wired boxes that are inert today, and it only ever writes the box's own
--    classname -- the same value the 192 working boxes hold.
UPDATE furniture_definitions
SET logic = name
WHERE name REGEXP '^wf_(act|cnd|trg|xtra)_'
  AND logic = 'furniture_multistate';

-- 4. Check. Every wired box should now carry a logic, and the count of inert ones should be zero.
SELECT SUM(logic = 'furniture_multistate') AS inertes_restantes,
       SUM(logic = name)                   AS liees_a_leur_classname,
       COUNT(*)                            AS total_boites_wired
FROM furniture_definitions
WHERE name REGEXP '^wf_(act|cnd|trg|xtra)_';

-- To undo:
--   UPDATE furniture_definitions d
--     JOIN wired_logic_binding_backup b ON b.definition_id = d.id
--      SET d.logic = b.logic_before;
