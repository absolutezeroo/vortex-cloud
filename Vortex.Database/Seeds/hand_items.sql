-- Hand items a pet will take, and what it gets out of them.
--
-- The ids and the names are the client's own, read off `handitem<N>` in the hotel's
-- external_flash_texts. The nutrition and thirst values are this hotel's decision and are meant to
-- be edited: a drink slakes thirst, food feeds, and nothing here restores energy — a nap does that,
-- the same rule `pet_food` follows.
--
-- Only consumables are listed. A hand item with no row is still held and still passed from hand to
-- hand; a pet just will not take it, which is right for a camera or a bunch of roses.

INSERT INTO `hand_items` (`id`, `hand_item_id`, `name`, `nutrition`, `thirst`) VALUES
    (1,  1,  'Refreshing Tea',        0, 8),
    (2,  2,  'Juice',                 0, 10),
    (3,  3,  'Carrot',                12, 0),
    (4,  4,  'Vanilla ice-cream',     8, 2),
    (5,  5,  'Milk',                  4, 8),
    (6,  6,  'Blackcurrant',          0, 10),
    (7,  7,  'Water',                 0, 15),
    (8,  8,  'Regular coffee',        0, 6),
    (9,  9,  'Decaff',                0, 6),
    (10, 10, 'Latte',                 2, 6),
    (11, 11, 'Mocha',                 2, 6),
    (12, 12, 'Macchiato',             2, 6),
    (13, 13, 'Espresso',              0, 5),
    (14, 14, 'Black coffee',          0, 6),
    (15, 15, 'Hot chocolate',         4, 6),
    (16, 16, 'Cappuccino',            2, 6),
    (17, 17, 'Java',                  0, 6),
    (18, 18, 'Tap water',             0, 15),
    (19, 19, 'Habbo Cola',            0, 10),
    (21, 21, 'Hamburger',             20, 0),
    (22, 22, 'Lime Habbo Soda',       0, 10),
    (23, 23, 'Beetroot Habbo Soda',   0, 10),
    (24, 24, 'Bubble juice from 1978',0, 10),
    (26, 26, 'Calippo',               6, 4),
    (27, 27, 'Arab tea',              0, 8),
    (29, 29, 'Tomato juice',          2, 10),
    (32, 32, 'Coconut Delight',       8, 4),
    (33, 33, '711 Cola',              0, 10),
    (34, 34, 'Fish',                  18, 0),
    (36, 36, 'Pear',                  10, 4),
    (37, 37, 'Peach',                 10, 4),
    (38, 38, 'Orange',                10, 4),
    (39, 39, 'Cheese slice',          12, 0),
    (40, 40, 'Orange Juice',          0, 10),
    (42, 42, 'Orange juice',          0, 10),
    (43, 43, 'Chilled Soda',          0, 10),
    (48, 48, 'Lolly Pop',             6, 0),
    (49, 49, 'Yoghurt Jar',           10, 2),
    (50, 50, 'Bubble Juice Bottle',   0, 10),
    (52, 52, 'Cheetos',               8, 0),
    (54, 54, 'Cereal Bowl',           14, 0),
    (55, 55, 'Pepsi bottle',          0, 10),
    (57, 57, 'Cherry Soda',           0, 10),
    (60, 60, 'Chestnuts',             10, 0);
