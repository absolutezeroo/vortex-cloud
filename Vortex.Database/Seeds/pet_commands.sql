-- ---------------------------------------------------------------------------
-- pet_commands seed
--
-- Command IDs (client key: pet.command.{id}):
--   0 = Sit          (level 0)  posture=sit   energy=5   xp=3
--   1 = Stand        (level 0)  posture=std   energy=5   xp=3
--   2 = Lay Down     (level 0)  posture=lay   energy=5   xp=3
--   3 = Speak        (level 0)  posture=spk   energy=5   xp=3
--   4 = Free         (level 0)  posture=      energy=0   xp=2
--   5 = Sleep        (level 2)  posture=lay   energy=0   xp=1
--   6 = Jump         (level 4)  posture=jmp   energy=10  xp=5
--   7 = Roll Over    (level 6)  posture=rll   energy=10  xp=5
--   8 = Play Dead    (level 8)  posture=ded   energy=10  xp=5
--   9 = Back Flip    (level 10) posture=flp   energy=15  xp=8
--
-- Pet types 0-13  : standard pets — full command set (0-9)
-- Pet type  14    : monster/exotic — basic commands only (0-4)
-- Pet type  15    : horse — riding commands (0-4) + jump (5)
-- Pet types 16-28 : exotic/special — basic commands only (0-4)
-- ---------------------------------------------------------------------------

INSERT INTO `pet_commands`
    (`pet_type`, `command`, `level_required`, `posture`, `energy_cost`, `xp_reward`, `created_at`, `updated_at`)
SELECT pet_type, command, level_required, posture, energy_cost, xp_reward, NOW(), NOW()
FROM (
    SELECT t.pet_type, c.command, c.level_required, c.posture, c.energy_cost, c.xp_reward
    FROM (
        SELECT 0 AS command, 0 AS level_required, 'sit' AS posture, 5 AS energy_cost, 3 AS xp_reward UNION ALL
        SELECT 1, 0,  'std',  5,  3 UNION ALL
        SELECT 2, 0,  'lay',  5,  3 UNION ALL
        SELECT 3, 0,  'spk',  5,  3 UNION ALL
        SELECT 4, 0,  '',     0,  2 UNION ALL
        SELECT 5, 2,  'lay',  0,  1 UNION ALL
        SELECT 6, 4,  'jmp', 10,  5 UNION ALL
        SELECT 7, 6,  'rll', 10,  5 UNION ALL
        SELECT 8, 8,  'ded', 10,  5 UNION ALL
        SELECT 9, 10, 'flp', 15,  8
    ) c
    CROSS JOIN (
        SELECT 0 AS pet_type UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL
        SELECT 4             UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL
        SELECT 8             UNION ALL SELECT 9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL
        SELECT 12            UNION ALL SELECT 13
    ) t
    WHERE c.command <= 9

    UNION ALL

    -- Pet type 14 — exotic (basic only: 0-4)
    SELECT t.pet_type, c.command, c.level_required, c.posture, c.energy_cost, c.xp_reward
    FROM (
        SELECT 0 AS command, 0 AS level_required, 'sit' AS posture, 5 AS energy_cost, 3 AS xp_reward UNION ALL
        SELECT 1, 0, 'std', 5,  3 UNION ALL
        SELECT 2, 0, 'lay', 5,  3 UNION ALL
        SELECT 3, 0, 'spk', 5,  3 UNION ALL
        SELECT 4, 0, '',    0,  2
    ) c
    CROSS JOIN (SELECT 14 AS pet_type) t

    UNION ALL

    -- Pet type 15 — horse (0-4 + jump as command 5)
    SELECT t.pet_type, c.command, c.level_required, c.posture, c.energy_cost, c.xp_reward
    FROM (
        SELECT 0 AS command, 0 AS level_required, 'sit' AS posture, 5 AS energy_cost, 3 AS xp_reward UNION ALL
        SELECT 1, 0, 'std', 5,  3 UNION ALL
        SELECT 2, 0, 'lay', 5,  3 UNION ALL
        SELECT 3, 0, 'spk', 5,  3 UNION ALL
        SELECT 4, 0, '',    0,  2 UNION ALL
        SELECT 5, 2, 'jmp', 10, 5
    ) c
    CROSS JOIN (SELECT 15 AS pet_type) t

    UNION ALL

    -- Pet types 16-28 — special/exotic (basic only: 0-4)
    SELECT t.pet_type, c.command, c.level_required, c.posture, c.energy_cost, c.xp_reward
    FROM (
        SELECT 0 AS command, 0 AS level_required, 'sit' AS posture, 5 AS energy_cost, 3 AS xp_reward UNION ALL
        SELECT 1, 0, 'std', 5,  3 UNION ALL
        SELECT 2, 0, 'lay', 5,  3 UNION ALL
        SELECT 3, 0, 'spk', 5,  3 UNION ALL
        SELECT 4, 0, '',    0,  2
    ) c
    CROSS JOIN (
        SELECT 16 AS pet_type UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL
        SELECT 20             UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL
        SELECT 24             UNION ALL SELECT 25 UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL
        SELECT 28
    ) t
) AS src
ON DUPLICATE KEY UPDATE
    level_required = src.level_required,
    posture        = src.posture,
    energy_cost    = src.energy_cost,
    xp_reward      = src.xp_reward,
    updated_at     = NOW();
