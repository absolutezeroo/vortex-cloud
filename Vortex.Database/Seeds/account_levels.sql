-- The account level ladder shown on the profile, keyed off the achievement score.
--
-- The client has no ladder of its own: it prints whatever number the server sends next to "Level",
-- so this table is the whole progression. Level 1 is the floor every new account starts on, which
-- is why it sits at score 0 rather than being implied.
--
-- The curve is deliberately gentle at the start (a handful of achievements gets you moving) and
-- widens after level 10, so the number keeps meaning something for a player who has been around a
-- while. These are this hotel's numbers and are meant to be edited.

INSERT IGNORE INTO `account_levels` (`id`, `level_number`, `required_score`) VALUES
    (1,  1,      0),
    (2,  2,     50),
    (3,  3,    125),
    (4,  4,    250),
    (5,  5,    450),
    (6,  6,    700),
    (7,  7,   1000),
    (8,  8,   1400),
    (9,  9,   1900),
    (10, 10,  2500),
    (11, 11,  3500),
    (12, 12,  5000),
    (13, 13,  7000),
    (14, 14,  9500),
    (15, 15, 12500),
    (16, 16, 16000),
    (17, 17, 20000),
    (18, 18, 25000),
    (19, 19, 31000),
    (20, 20, 40000);
