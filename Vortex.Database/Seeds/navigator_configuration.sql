-- Navigator reference data: the tabs of the left pane, the quick-link buttons under each tab, and
-- the flat categories a room can be filed under.
--
-- Nothing seeded these tables before, so a fresh hotel booted with zero top-level contexts: the
-- client's NavigatorMetaData arrived empty and the navigator rendered no tabs at all.
--
-- The search codes are the client's own, taken from the localization keys it looks a result block's
-- title up with (navigator.searchcode.title.<code> in gamedata/external_texts.json). A code that is
-- not in that list renders with the raw code as its header.
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

-- ── Top-level contexts (the navigator's tabs) ────────────────────────────────
INSERT IGNORE INTO `navigator_top_level_contexts`
    (`id`, `search_code`, `visible`, `query_type`, `order_num`)
VALUES
    (1, 'official-root', 1, 13, 1),  -- StaffPicks
    (2, 'hotel_view',    1, 10, 2),  -- Popular
    (3, 'myworld_view',  1,  1, 3);  -- MyRooms

-- ── Quick links (the buttons inside a tab) ───────────────────────────────────
-- `filter` is appended to the button's caption by the client (QuickLinksView), so it stays empty
-- unless the extra text is wanted on screen. `localization` is unused by this client revision.
INSERT IGNORE INTO `navigator_quick_links`
    (`id`, `top_level_context_id`, `search_code`, `filter`, `localization`, `query_type`, `order_num`)
VALUES
    -- Public rooms
    (1,  1, 'official',      '', '', 13, 1),  -- StaffPicks

    -- Hotel
    (2,  2, 'popular',       '', '', 10, 1),  -- Popular
    (3,  2, 'highest_score', '', '', 11, 2),  -- HighestScore
    (4,  2, 'staffpicks',    '', '', 13, 3),  -- StaffPicks
    (5,  2, 'recommended',   '', '', 12, 4),  -- Recommended
    (6,  2, 'new_ads',       '', '',  8, 5),  -- RoomAds
    (7,  2, 'groups',        '', '', 16, 6),  -- GuildBases
    (8,  2, 'categories',    '', '',  9, 7),  -- ByFlatCategory

    -- My world
    (9,  3, 'my',            '', '',  1, 1),  -- MyRooms
    (10, 3, 'favorites',     '', '',  2, 2),  -- MyFavorites
    (11, 3, 'history_freq',  '', '',  6, 3),  -- FrequentHistory
    (12, 3, 'history',       '', '',  5, 4),  -- History
    (13, 3, 'with_rights',   '', '',  7, 5),  -- WithRights
    (14, 3, 'friends_rooms', '', '',  3, 6),  -- FriendsRooms
    (15, 3, 'with_friends',  '', '',  4, 7),  -- WithFriends
    (16, 3, 'my_groups',     '', '', 15, 8);  -- MyGroups
