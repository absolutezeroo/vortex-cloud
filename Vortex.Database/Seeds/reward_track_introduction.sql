-- The Introduction Track: the official onboarding campaign, as content.
--
-- Every task id and the track id itself are the client's own, read from `external_flash_texts`
-- (`reward_track.introduction.task.<id>.name`). They are not free choices: the client builds its
-- localization key from the track id and the task id, so `visit_rooms` renders "Visit rooms" and
-- anything else renders the raw key.
--
-- THIS FILE IS THE PROOF, NOT THE FEATURE. Nothing in Vortex.RewardTracks mentions the introduction
-- track, its tasks, or any of the numbers here. It is rows, and the next campaign is different rows
-- written from the dashboard. The engine could not tell you which of its tracks is the onboarding
-- one.
--
-- WHAT IS SEEDED AND WHAT IS NOT. The client's texts name 30 introduction tasks. Fifteen are here:
-- the ones an accepted gameplay action actually raises today. The other fifteen -- go_swimming,
-- grab_drink, pet_a_pet, feed_pet, level_pet, use_teleport, close_love_lock, publish_picture,
-- follow_friend, send_messenger_invite, set_relationship_status, replenish_respect, use_furniture,
-- rotate_furniture, place_builders_club_furni -- have no domain event behind them yet. Seeding them
-- would ship tasks that can never advance, which is worse than shipping fewer: a player would stare
-- at a bar that never moves. Each becomes one row plus one event handler the day its signal exists.
--
-- POINTS BUDGET, so the milestones are reachable rather than hopeful:
--   free stages          410
--   + premium task        30
--   + premium instant     50
--   premium ceiling      490
-- The highest free prize needs 410 and the highest premium prize 400. The content validator checks
-- exactly this and refuses to publish a track whose top milestone is out of reach.
--
-- Ids 1..N are fixed so the migration is re-runnable. INSERT IGNORE throughout: a hotel that has
-- re-tuned these numbers keeps its edits.

-- ---------------------------------------------------------------------------------------------
-- The track
-- ---------------------------------------------------------------------------------------------
-- Permanent (no dates), always unlocked, blue, and completed when every free prize is claimed --
-- which is also what the client shows as "complete", so the two agree.
INSERT IGNORE INTO reward_tracks
    (id, track_id, theme, status, sort_order, unlock_kind, unlock_value, completion_policy,
     premium_enabled, premium_boost_permille, premium_instant_points, premium_cost_credits,
     premium_cost_diamonds, content_version, hidden, campaign_code, created_at)
VALUES
    (1, 'introduction', 'blue', 2, 10, 0, '', 0,
     1, 1200, 50, 0,
     25, 1, 0, 'onboarding', UTC_TIMESTAMP());

-- ---------------------------------------------------------------------------------------------
-- Tasks
-- ---------------------------------------------------------------------------------------------
-- mode: 0 counter, 1 distinct, 2 absolute, 3 highest.
-- `visit_rooms` is the only distinct one here: the client's text says "Explore rooms made by other
-- players", and a counter would be satisfied by walking in and out of the same door twenty times.
--
-- TASK_ID AND ACTION_CODE ARE TWO DIFFERENT VOCABULARIES AND MUST NOT BE COPIED FROM ONE ANOTHER.
-- `task_id` is the localization stem: the client renders `reward_track.introduction.task.<task_id>.name`,
-- so it has to be one of the thirty ids in `external_flash_texts` -- `visit_rooms`, `change_outfit`,
-- and so on. `action_code` is the ARTWORK key: `RewardTrackTaskRowView.as` builds the icon name as
-- `"reward_track_tasks_" + actionType.toLowerCase()`, so it has to be one of the thirty
-- `reward_track_tasks_*` embeds in `HabboWindowManagerCom.as` -- `enter_other_users_room`,
-- `change_figure`. The two lists overlap in name but not in content, and this seed originally
-- reused the first for the second: every task in the track drew a blank square and logged
-- `ResourceManager: Asset not found`.
INSERT IGNORE INTO reward_track_tasks
    (id, reward_track_id, task_id, action_code, parameter, mode, premium, sort_order, created_at)
VALUES
    (1,  1, 'visit_rooms',            'enter_other_users_room', '', 1, 0, 10,  UTC_TIMESTAMP()),
    (2,  1, 'chat_with_users',        'chat_with_someone',      '', 0, 0, 20,  UTC_TIMESTAMP()),
    (3,  1, 'make_friends',           'request_friend',         '', 0, 0, 30,  UTC_TIMESTAMP()),
    (4,  1, 'give_respect',           'give_respect',           '', 0, 0, 40,  UTC_TIMESTAMP()),
    (5,  1, 'change_outfit',          'change_figure',          '', 0, 0, 50,  UTC_TIMESTAMP()),
    (6,  1, 'change_motto',           'change_motto',           '', 0, 0, 60,  UTC_TIMESTAMP()),
    (7,  1, 'create_room',            'create_room',            '', 0, 0, 70,  UTC_TIMESTAMP()),
    (8,  1, 'place_furniture',        'place_item',             '', 0, 0, 80,  UTC_TIMESTAMP()),
    (9,  1, 'move_furniture',         'move_item',              '', 0, 0, 90,  UTC_TIMESTAMP()),
    (10, 1, 'buy_catalog_furni',      'buy_from_catalogue',     '', 0, 0, 100, UTC_TIMESTAMP()),
    (11, 1, 'wear_badge',             'wear_badge',             '', 0, 0, 110, UTC_TIMESTAMP()),
    (12, 1, 'use_habbicon',           'use_habbicon',           '', 0, 0, 120, UTC_TIMESTAMP()),
    (13, 1, 'dance_in_room',          'dance',                  '', 0, 0, 130, UTC_TIMESTAMP()),
    (14, 1, 'wave_at_user',           'wave',                   '', 0, 0, 140, UTC_TIMESTAMP()),
    (15, 1, 'send_messenger_message', 'send_messenger_message', '', 0, 0, 150, UTC_TIMESTAMP()),
    -- Premium-only. A free player sees it locked, which is half the reason anyone buys premium, and
    -- it does not advance for them at all.
    --
    -- The only row here that is ours rather than Habbo's, and it shows: `complete_trade` is in
    -- neither vocabulary, so this task renders its raw localization key and draws no icon. Left as
    -- it is on purpose -- a made-up id would still have no text, and borrowing an unrelated icon
    -- would say the task is something it is not.
    (16, 1, 'complete_trade',         'complete_trade',         '', 0, 1, 160, UTC_TIMESTAMP());

-- ---------------------------------------------------------------------------------------------
-- Task stages
-- ---------------------------------------------------------------------------------------------
-- level_index is zero-based and ascends by required_count. `chat_with_users` is the four-stage one
-- the brief describes (5/20/50/200 for 10/20/30/40); the rest are one, two or three stages.
INSERT IGNORE INTO reward_track_task_levels
    (id, task_id, level_index, required_count, points_reward, premium, created_at)
VALUES
    (1,  1,  0, 1,   10, 0, UTC_TIMESTAMP()),
    (2,  1,  1, 5,   20, 0, UTC_TIMESTAMP()),
    (3,  1,  2, 20,  30, 0, UTC_TIMESTAMP()),

    (4,  2,  0, 5,   10, 0, UTC_TIMESTAMP()),
    (5,  2,  1, 20,  20, 0, UTC_TIMESTAMP()),
    (6,  2,  2, 50,  30, 0, UTC_TIMESTAMP()),
    (7,  2,  3, 200, 40, 0, UTC_TIMESTAMP()),

    (8,  3,  0, 1,   10, 0, UTC_TIMESTAMP()),
    (9,  3,  1, 5,   20, 0, UTC_TIMESTAMP()),

    (10, 4,  0, 1,   10, 0, UTC_TIMESTAMP()),
    (11, 4,  1, 10,  20, 0, UTC_TIMESTAMP()),

    (12, 5,  0, 1,   10, 0, UTC_TIMESTAMP()),
    (13, 6,  0, 1,   10, 0, UTC_TIMESTAMP()),
    (14, 7,  0, 1,   20, 0, UTC_TIMESTAMP()),

    (15, 8,  0, 1,   10, 0, UTC_TIMESTAMP()),
    (16, 8,  1, 25,  20, 0, UTC_TIMESTAMP()),

    (17, 9,  0, 1,   10, 0, UTC_TIMESTAMP()),
    (18, 10, 0, 1,   20, 0, UTC_TIMESTAMP()),
    (19, 11, 0, 1,   10, 0, UTC_TIMESTAMP()),

    (20, 12, 0, 1,   10, 0, UTC_TIMESTAMP()),
    (21, 12, 1, 10,  20, 0, UTC_TIMESTAMP()),

    (22, 13, 0, 1,   10, 0, UTC_TIMESTAMP()),
    (23, 14, 0, 1,   10, 0, UTC_TIMESTAMP()),

    (24, 15, 0, 1,   10, 0, UTC_TIMESTAMP()),
    (25, 15, 1, 10,  20, 0, UTC_TIMESTAMP()),

    (26, 16, 0, 1,   30, 1, UTC_TIMESTAMP());

-- ---------------------------------------------------------------------------------------------
-- Milestones
-- ---------------------------------------------------------------------------------------------
-- A prize knows a points threshold and what it hands over, and nothing about which task paid for
-- the points. That is what lets the tasks above be rewritten without touching a single prize.
INSERT IGNORE INTO reward_track_prizes
    (id, reward_track_id, prize_id, required_points, premium, sort_order, created_at)
VALUES
    (1, 1, 'intro_free_1', 20,  0, 10, UTC_TIMESTAMP()),
    (2, 1, 'intro_free_2', 50,  0, 20, UTC_TIMESTAMP()),
    (3, 1, 'intro_free_3', 100, 0, 30, UTC_TIMESTAMP()),
    (4, 1, 'intro_free_4', 180, 0, 40, UTC_TIMESTAMP()),
    (5, 1, 'intro_free_5', 260, 0, 50, UTC_TIMESTAMP()),
    (6, 1, 'intro_free_6', 410, 0, 60, UTC_TIMESTAMP()),

    (7, 1, 'intro_prem_1', 40,  1, 15, UTC_TIMESTAMP()),
    (8, 1, 'intro_prem_2', 120, 1, 35, UTC_TIMESTAMP()),
    (9, 1, 'intro_prem_3', 240, 1, 45, UTC_TIMESTAMP()),
    (10,1, 'intro_prem_4', 400, 1, 55, UTC_TIMESTAMP());

-- ---------------------------------------------------------------------------------------------
-- What the milestones hand over
-- ---------------------------------------------------------------------------------------------
-- kind is the client's own product-type numbering: 8 currency, 12 habbicon, 100 entitlement.
-- For currency, reward_type_id is the activity-point type: -1 credits, 0 duckets, 5 diamonds.
-- For a Habbicon it is the `habbicons.id`, so it moved with the 2026-09-05 asset-pack alignment:
-- 29 is duck_happy, 39 duck_cool, 51 frank_smile, 59 frank_wink (they were 7, 2, 29 and 33 under
-- our old numbering, where 29 and 33 meant two entirely different icons).
--
-- No furniture. A furniture id has to exist in this hotel's furnidata, and seeding one that does
-- not would be a milestone that fails to deliver -- exactly the failure the grant pipeline reports
-- and nobody wants to see on a fresh install. Currency, Habbicons and the trading pass all exist
-- by construction. An operator adds furniture from the dashboard, where they can pick a real one.
INSERT IGNORE INTO reward_track_prize_rewards
    (id, prize_id, kind, reward_type_id, amount, extra_params, sort_order, created_at)
VALUES
    (1,  1,  8,  '0',     100, '', 0, UTC_TIMESTAMP()),
    (2,  2,  12, '29',    1,   '', 0, UTC_TIMESTAMP()),
    (3,  3,  8,  '0',     250, '', 0, UTC_TIMESTAMP()),
    (4,  4,  12, '39',    1,   '', 0, UTC_TIMESTAMP()),
    -- The trading pass. The official track's own headline reward, and the reason RewardKind has an
    -- entitlement member at all: it sets the account's TRADE perk, and trading reads the perk it
    -- already read. Reward tracks never learn what trading is.
    (5,  5,  100,'TRADE', 1,   '', 0, UTC_TIMESTAMP()),
    (6,  6,  8,  '0',     500, '', 0, UTC_TIMESTAMP()),

    (7,  7,  8,  '-1',    50,  '', 0, UTC_TIMESTAMP()),
    (8,  8,  12, '51',    1,   '', 0, UTC_TIMESTAMP()),
    (9,  9,  8,  '-1',    150, '', 0, UTC_TIMESTAMP()),
    -- A bundle: one milestone, three rewards, one claim. The client draws the first (the Habbicon)
    -- and all three are granted together.
    (10, 10, 12, '59',    1,   '', 0, UTC_TIMESTAMP()),
    (11, 10, 8,  '0',     300, '', 1, UTC_TIMESTAMP()),
    (12, 10, 8,  '-1',    100, '', 2, UTC_TIMESTAMP());
