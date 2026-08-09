-- Navigator reference data: the tabs of the left pane, the quick-link buttons under each tab, the
-- flat categories a room can be filed under, and the event categories a room ad can be filed under.
--
-- Nothing seeded these tables before, so a fresh hotel booted with zero top-level contexts: the
-- client's NavigatorMetaData arrived empty and the navigator rendered no tabs at all.
--
-- The search codes are the client's own, taken from the localization keys it looks a result block's
-- title up with (navigator.searchcode.title.<code> in gamedata/external_texts.json). A code that is
-- not in that list renders with the raw code as its header.
--
-- A tab is an overview: the server answers it with one block per quick link below, so the quick-link
-- rows are what decides whether "My World" shows one list or the eight it should.
--
-- query_type values are Vortex.Primitives.Navigator.Enums.NavigatorQueryType.
-- INSERT IGNORE throughout: safe to re-run, and an operator's own rows win.

-- ── Flat categories ──────────────────────────────────────────────────────────
-- global_category is a client localization key (navigator.flatcategory.global.<KEY>); when it is
-- set the client renders that instead of `name`, so `name` here is only the operator-facing label.
INSERT IGNORE INTO `navigator_flatcats`
    (`id`, `name`, `visible`, `automatic`, `automatic_category`, `global_category`, `staff_only`, `min_rank`, `order_num`)
VALUES
    (1,  'Personal Space',          1, 0, '', 'PERSONAL', 0, 1, 1),
    (2,  'Chat and discussion',     1, 0, '', 'CHAT',     0, 1, 2),
    (3,  'Party',                   1, 0, '', 'PARTY',    0, 1, 3),
    (4,  'Trading',                 1, 0, '', 'TRADING',  0, 1, 4),
    (5,  'Games',                   1, 0, '', 'GAMES',    0, 1, 5),
    (6,  'Building and decoration', 1, 0, '', 'BUILDING', 0, 1, 6),
    (7,  'Help Centers',            1, 0, '', 'HELP',     0, 1, 7),
    (8,  'Fansite Square',          1, 0, '', 'FANSITE',  0, 1, 8),
    (9,  'Builders'' Club',         1, 0, '', 'BC',       0, 1, 9),
    -- Staff-only: a player must not be able to file their own room as a public room.
    (10, 'Public Rooms',            1, 0, '', 'OFFICIAL', 1, 1, 10);

-- ── Event categories ─────────────────────────────────────────────────────────
-- The ids are load-bearing: the client addresses these with an `eventcategory__<id>` search code and
-- localizes them with navigator.searchcode.title.eventcategory__<id>, so the numbering below is the
-- client's, not ours. `name` is the operator-facing label.
INSERT IGNORE INTO `navigator_eventcats` (`id`, `name`, `visible`)
VALUES
    (1,  'Hottest Events',          1),
    (2,  'Parties & Music',         1),
    (3,  'Role Play',               1),
    (4,  'Help Desk',               1),
    (5,  'Trading',                 1),
    (6,  'Games',                   1),
    (7,  'Debates & Discussions',   1),
    (8,  'Grand Openings',          1),
    (9,  'Friending',               1),
    (10, 'Jobs',                    1),
    (11, 'Group Events',            1);

-- ── Top-level contexts (the navigator's tabs) ────────────────────────────────
INSERT IGNORE INTO `navigator_top_level_contexts`
    (`id`, `search_code`, `visible`, `query_type`, `order_num`)
VALUES
    (1, 'official_view', 1, 13, 1),  -- StaffPicks
    (2, 'hotel_view',    1, 10, 2),  -- Popular
    (3, 'roomads_view',  1,  8, 3),  -- RoomAds
    (4, 'myworld_view',  1,  1, 4);  -- MyRooms

-- ── Quick links (the blocks inside a tab) ────────────────────────────────────
-- `filter` is appended to the button's caption by the client (QuickLinksView), so it stays empty
-- unless the extra text is wanted on screen. `localization` is unused by this client revision.
INSERT IGNORE INTO `navigator_quick_links`
    (`id`, `top_level_context_id`, `search_code`, `filter`, `localization`, `query_type`, `order_num`)
VALUES
    -- Public rooms
    (1,  1, 'official',            '', '', 13, 1),  -- StaffPicks

    -- Hotel
    (2,  2, 'popular',             '', '', 10, 1),  -- Popular
    (3,  2, 'highest_score',       '', '', 11, 2),  -- HighestScore
    (4,  2, 'staffpicks',          '', '', 13, 3),  -- StaffPicks
    (5,  2, 'recommended',         '', '', 12, 4),  -- Recommended
    (6,  2, 'groups',              '', '', 16, 5),  -- GuildBases
    -- Expands in place into one block per flat category.
    (7,  2, 'categories',          '', '',  9, 6),  -- ByFlatCategory

    -- Events / room ads
    (8,  3, 'new_ads',             '', '',  8, 1),  -- RoomAds
    (9,  3, 'eventcategory__1',    '', '', 18, 2),  -- EventCategory
    (10, 3, 'eventcategory__2',    '', '', 18, 3),
    (11, 3, 'eventcategory__3',    '', '', 18, 4),
    (12, 3, 'eventcategory__4',    '', '', 18, 5),
    (13, 3, 'eventcategory__5',    '', '', 18, 6),
    (14, 3, 'eventcategory__6',    '', '', 18, 7),
    (15, 3, 'eventcategory__7',    '', '', 18, 8),
    (16, 3, 'eventcategory__8',    '', '', 18, 9),
    (17, 3, 'eventcategory__9',    '', '', 18, 10),
    (18, 3, 'eventcategory__10',   '', '', 18, 11),
    (19, 3, 'eventcategory__11',   '', '', 18, 12),

    -- My world
    (20, 4, 'my',                  '', '',  1, 1),  -- MyRooms
    (21, 4, 'favorites',           '', '',  2, 2),  -- MyFavorites
    (22, 4, 'history_freq',        '', '',  6, 3),  -- FrequentHistory
    (23, 4, 'history',             '', '',  5, 4),  -- History
    (24, 4, 'with_rights',         '', '',  7, 5),  -- WithRights
    (25, 4, 'friends_rooms',       '', '',  3, 6),  -- FriendsRooms
    (26, 4, 'with_friends',        '', '',  4, 7),  -- WithFriends
    (27, 4, 'my_groups',           '', '', 15, 8);  -- MyGroups
