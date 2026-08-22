-- The three contract furni carry a logic that cannot say what they are.
--
-- wf_contract_payment, wf_contract_reward and wf_contract_trade ship bound to `furniture_basic`,
-- which is the plain floor behaviour shared with roughly 5 800 other definitions. That is correct
-- as behaviour and useless as identity: the wired boxes that offer and cancel transactions have to
-- pick the contract out of the furni they were pointed at, and the only reliable way to ask what a
-- furni is, is its logic. A classname is not a key in this database.
--
-- FurnitureContractLogic registers under the three names below and inherits the plain floor
-- behaviour whole, so nothing changes about how a contract acts -- it only becomes recognisable.
--
-- Same shape as scripts/sql/wired_logic_binding_fix.sql: read the dry run, keep the way back, then
-- write.

-- 1. DRY RUN.
SELECT id, name AS classname, logic AS logic_now, name AS logic_after
FROM furniture_definitions
WHERE name IN ('wf_contract_payment', 'wf_contract_reward', 'wf_contract_trade')
  AND logic <> name
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
WHERE name IN ('wf_contract_payment', 'wf_contract_reward', 'wf_contract_trade')
  AND logic <> name
ON DUPLICATE KEY UPDATE logic_before = VALUES(logic_before);

-- 3. The fix.
UPDATE furniture_definitions
SET logic = name
WHERE name IN ('wf_contract_payment', 'wf_contract_reward', 'wf_contract_trade')
  AND logic <> name;

-- 4. Check.
SELECT name, logic
FROM furniture_definitions
WHERE name IN ('wf_contract_payment', 'wf_contract_reward', 'wf_contract_trade')
ORDER BY name;

-- To undo:
--   UPDATE furniture_definitions d
--     JOIN wired_logic_binding_backup b ON b.definition_id = d.id
--      SET d.logic = b.logic_before;
