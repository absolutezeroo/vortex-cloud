-- Two furniture families that never worked, for the same reason in two shapes: the definition row
-- names behaviour that neither side binds, so the furni is inert with no error a player can see.
--
-- Re-runnable and self-guarding: every statement is keyed on the outcome, not on a flag, so a
-- half-applied hotel converges rather than skipping.

-- ---------------------------------------------------------------------------------------------
-- Crackables
-- ---------------------------------------------------------------------------------------------
-- 353 definitions run the crackable family, and five of them work. The rest fail in one of two
-- ways:
--
--   * 314 carry `furniture_crackable` on the legacy stuff-data format. RoomCrackableSystem casts
--     the item's stuff data to ICrackableStuffData and gives up when it is not, so the hits have
--     nowhere to be counted -- the exact case its "does not carry crackable stuff data" warning
--     describes.
--   * 34 carry `crackable`, which is the Arcturus dump's interaction_type, not a logic name.
--     Nothing is registered under it on either side, so those definitions resolve to the plain
--     floor default and a hit is just a click.
--
-- The asset pack is not the authority here and saying so matters, because the repo's usual rule is
-- that it is: 55 029 of its 72 991 entries declare `furniture_multistate`, including known-good
-- crackables like wonderland_c25_crackableb that the curated seed had to override by hand. The
-- client derives crackable behaviour from the data format it is sent, not from the logic string, so
-- the format is what has to be right.
UPDATE `furniture_definitions`
   SET `logic` = 'furniture_crackable',
       `stuff_data_type` = 7
 WHERE `logic` IN ('crackable', 'furniture_crackable')
   AND (`logic` <> 'furniture_crackable' OR `stuff_data_type` <> 7);

-- A crackable also needs somewhere to draw its prize from and a number of hits to take, neither of
-- which exists in furnidata or in the assets -- total_states is not it either, the five curated
-- crackables all declare 1 state and take 6 to 10 hits. So the count below is a stated default, not
-- a derived value: ten hits, the most common of the five, on the starter pool. Every one of them is
-- retunable from the dashboard's Prize Pools page, which is what that page is for.
--
-- Only definitions with no binding at all are touched, so the curated five keep their tuned counts.
INSERT INTO `prize_pool_bindings` (`furniture_definition_id`, `pool_id`, `hits_required`, `enabled`)
    SELECT d.`id`, pool.`id`, 10, 1
      FROM `furniture_definitions` d
      JOIN `prize_pools` pool ON pool.`code` = 'crackable-starter'
     WHERE d.`logic` = 'furniture_crackable'
       AND d.`deleted_at` IS NULL
       AND NOT EXISTS (
           SELECT 1 FROM (SELECT * FROM `prize_pool_bindings`) b
            WHERE b.`furniture_definition_id` = d.`id`
       );

-- ---------------------------------------------------------------------------------------------
-- High-score boards
-- ---------------------------------------------------------------------------------------------
-- The twelve scoreboards (highscore_classic, highscore_mostwin, highscore_perteam, four variants
-- each) carry `wf_highscore` -- again the Arcturus interaction_type. Their assets declare
-- `furniture_high_score`, which is what the client's factory switches on to build the logic that
-- raises ROWRE_HIGH_SCORE_DISPLAY and opens the scoreboard widget.
--
-- They were missed by the asset-binding pass for a mechanical reason worth recording: the pass
-- matches asset classnames exactly, and these rows are named `highscore_classic*1` .. `*4` while the
-- asset is `highscore_classic`. Any definition whose name carries a `*variant` suffix was invisible
-- to it.
--
-- Format 6 goes with the rename: the widget reads HighScoreData, and StuffDataSnapshotSerializer
-- already writes that shape in the order the client parses it (string, score type, clear type, then
-- the entries). Nothing writes scores into it yet -- the boards will render empty until the wired
-- scoring subsystem fills them -- but an empty board is the furni the client is meant to draw,
-- where today it draws nothing at all.
UPDATE `furniture_definitions`
   SET `logic` = 'furniture_high_score',
       `stuff_data_type` = 6
 WHERE `logic` = 'wf_highscore'
   AND `deleted_at` IS NULL;
