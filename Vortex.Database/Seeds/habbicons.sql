-- Habbicon content: two collections and their members.
--
-- The codes are NOT invented. Every one below is read from the official client's own
-- `external_flash_texts` (`habbicon_collection_<code>_name`, `habbicon_<code>_name`), which is the
-- only place the real vocabulary exists -- there is no server dump. `duck2` ("Duckicons 2") appears
-- in the same file and is deliberately not seeded: its members are not listed there, so the set
-- would be empty and uncompletable.
--
-- WHAT IS A GUESS, AND IT MATTERS: **the ids**. The client resolves a Habbicon's artwork by id from
-- its own `habbicons.json` asset manifest, which we do not have. The ids below are 1..33 in the
-- order the codes appear in the texts file; if the real pack numbers them differently, every
-- picture in the album is the wrong one. An operator installing a real Habbicon asset pack must
-- align `habbicons.id` with that pack's numbering -- the codes are the anchor, the ids are ours.
--
-- `duck_spinning` is the Duckicons bonus. It is the one duck code whose name implies animation, and
-- the client's own book calls the set reward "animated" ("Collect sets, unlock animated Habbicons").
-- Frankicons is seeded WITHOUT a bonus on purpose: nothing in the texts names one, and inventing a
-- reward is worse than a set that simply has none -- the shop and the album both handle it.
--
-- Prices are ours. Ducks cost duckets (activity point type 0) and Franks cost credits, which is not
-- a design statement so much as proof that both currencies work. Every one is admin-editable.
--
-- INSERT IGNORE throughout: re-running this against a hotel that has re-priced or renumbered
-- anything changes nothing.

INSERT IGNORE INTO habbicon_collections
    (id, code, sort_order, enabled, hidden, price_credits, price_activity_points, activity_point_type, campaign_code, created_at)
VALUES
    (1, 'duck',  10, 1, 0, 0,  400, 0, '', UTC_TIMESTAMP()),
    (2, 'frank', 20, 1, 0, 90, 0,   0, '', UTC_TIMESTAMP());

-- Duckicons: 21 entries plus the animated bonus.
INSERT IGNORE INTO habbicons
    (id, code, collection_id, sort_order, is_collection_reward, price_credits, price_activity_points, activity_point_type, enabled, created_at)
VALUES
    (1,  'duck_angel',    1,  10, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (2,  'duck_cool',     1,  20, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (3,  'duck_devil',    1,  30, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (4,  'duck_devious',  1,  40, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (5,  'duck_duck',     1,  50, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (6,  'duck_grimace',  1,  60, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (7,  'duck_happy',    1,  70, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (8,  'duck_laughing', 1,  80, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (9,  'duck_love',     1,  90, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (10, 'duck_metal',    1, 100, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (11, 'duck_nohear',   1, 110, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (12, 'duck_nosay',    1, 120, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (13, 'duck_nosee',    1, 130, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (14, 'duck_party',    1, 140, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (15, 'duck_pleading', 1, 150, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (16, 'duck_pleased',  1, 160, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (17, 'duck_sad',      1, 170, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (18, 'duck_shock',    1, 180, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (19, 'duck_silly',    1, 190, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (20, 'duck_think',    1, 200, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (21, 'duck_wink',     1, 210, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    -- The bonus. Priced at nothing on purpose: it is claimed by completing the set, and the shop
    -- refuses to sell a collection reward whatever its price says.
    (22, 'duck_spinning', 1, 900, 1, 0, 0,  0, 1, UTC_TIMESTAMP());

-- Frankicons: 11 entries, no bonus.
INSERT IGNORE INTO habbicons
    (id, code, collection_id, sort_order, is_collection_reward, price_credits, price_activity_points, activity_point_type, enabled, created_at)
VALUES
    (23, 'frank_frank',     2,  10, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (24, 'frank_happy',     2,  20, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (25, 'frank_relief',    2,  30, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (26, 'frank_sad',       2,  40, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (27, 'frank_scared',    2,  50, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (28, 'frank_silly',     2,  60, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (29, 'frank_smile',     2,  70, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (30, 'frank_stareyes',  2,  80, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (31, 'frank_surprised', 2,  90, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (32, 'frank_thinking',  2, 100, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (33, 'frank_wink',      2, 110, 0, 10, 0, 0, 1, UTC_TIMESTAMP());
