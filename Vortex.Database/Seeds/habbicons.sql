-- Habbicon content: two collections and their members.
--
-- The codes are NOT invented. Every one below is read from the official client's own
-- `external_flash_texts` (`habbicon_collection_<code>_name`, `habbicon_<code>_name`), which is the
-- only place the real vocabulary exists -- there is no server dump. `duck2` ("Duckicons 2") appears
-- in the same file and is deliberately not seeded: its members are not listed there, so the set
-- would be empty and uncompletable.
--
-- THE IDS ARE NO LONGER A GUESS. They used to be 1..33, in the order the codes appear in the texts
-- file, with a warning that an operator installing a real asset pack would have to align them. A
-- pack landed on 2026-09-05 and they are aligned to it: `habbicons.json` numbers the 33 codes
-- 28..60, and the client resolves artwork by **id** alone (`HabbiconAssetManager` keys `_previewRects`
-- and `_definitions` by it), so the numbering is not free. All 33 codes matched the pack by name,
-- one for one, which is what made the remap mechanical. It was not cosmetic either: ids 28..33
-- existed on both sides with different meanings, so `frank_silly` (28, ours) drew the pack's
-- `duck_duck` -- wrong pictures, not missing ones.
--
-- The collection ids come from the same pack: its `collectionIcons` are 5, 6 and 7, and the icons
-- read (in order) a plain duck, a duck with a green scarf, and Frank. So `duck` is 5 and `frank` is
-- 7. The middle one is `duck2` -- "Duckicons 2", the set named in the texts file whose members are
-- not listed there, still deliberately unseeded for that reason.
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
    (5, 'duck',  10, 1, 0, 0,  400, 0, '', UTC_TIMESTAMP()),
    (7, 'frank', 20, 1, 0, 90, 0,   0, '', UTC_TIMESTAMP());

-- Duckicons: 21 entries plus the animated bonus.
INSERT IGNORE INTO habbicons
    (id, code, collection_id, sort_order, is_collection_reward, price_credits, price_activity_points, activity_point_type, enabled, created_at)
VALUES
    (36, 'duck_angel',    5,  10, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (39, 'duck_cool',     5,  20, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (37, 'duck_devil',    5,  30, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (43, 'duck_devious',  5,  40, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (28, 'duck_duck',     5,  50, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (42, 'duck_grimace',  5,  60, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (29, 'duck_happy',    5,  70, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (41, 'duck_laughing', 5,  80, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (49, 'duck_love',     5,  90, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (44, 'duck_metal',    5, 100, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (33, 'duck_nohear',   5, 110, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (35, 'duck_nosay',    5, 120, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (34, 'duck_nosee',    5, 130, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (48, 'duck_party',    5, 140, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (45, 'duck_pleading', 5, 150, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (40, 'duck_pleased',  5, 160, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (30, 'duck_sad',      5, 170, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (31, 'duck_shock',    5, 180, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (46, 'duck_silly',    5, 190, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (32, 'duck_think',    5, 200, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    (47, 'duck_wink',     5, 210, 0, 0, 25, 0, 1, UTC_TIMESTAMP()),
    -- The bonus. Priced at nothing on purpose: it is claimed by completing the set, and the shop
    -- refuses to sell a collection reward whatever its price says.
    (38, 'duck_spinning', 5, 900, 1, 0, 0,  0, 1, UTC_TIMESTAMP());

-- Frankicons: 11 entries, no bonus.
INSERT IGNORE INTO habbicons
    (id, code, collection_id, sort_order, is_collection_reward, price_credits, price_activity_points, activity_point_type, enabled, created_at)
VALUES
    (50, 'frank_frank',     7,  10, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (52, 'frank_happy',     7,  20, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (58, 'frank_relief',    7,  30, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (53, 'frank_sad',       7,  40, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (54, 'frank_scared',    7,  50, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (57, 'frank_silly',     7,  60, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (51, 'frank_smile',     7,  70, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (60, 'frank_stareyes',  7,  80, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (55, 'frank_surprised', 7,  90, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (56, 'frank_thinking',  7, 100, 0, 10, 0, 0, 1, UTC_TIMESTAMP()),
    (59, 'frank_wink',      7, 110, 0, 10, 0, 0, 1, UTC_TIMESTAMP());
