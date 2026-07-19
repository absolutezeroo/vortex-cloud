-- ---------------------------------------------------------------------------
-- pet_levels seed
--
-- 20 levels for all pet types (0-28).
-- XP required is shared across all types; caps scale with level.
-- energy_cap and nutrition_cap start at 100 and increase by 10 per level.
-- ---------------------------------------------------------------------------
INSERT INTO pet_levels (pet_type, level, experience_required, energy_cap, nutrition_cap)
SELECT pet_type, level, experience_required, energy_cap, nutrition_cap
FROM (
    SELECT
        t.pet_type,
        l.level,
        l.experience_required,
        100 + (l.level - 1) * 10 AS energy_cap,
        100 + (l.level - 1) * 10 AS nutrition_cap
    FROM (
        SELECT 0 AS level,  0     AS experience_required UNION ALL
        SELECT 1,    100  UNION ALL
        SELECT 2,    250  UNION ALL
        SELECT 3,    500  UNION ALL
        SELECT 4,    900  UNION ALL
        SELECT 5,   1400  UNION ALL
        SELECT 6,   2100  UNION ALL
        SELECT 7,   3000  UNION ALL
        SELECT 8,   4200  UNION ALL
        SELECT 9,   5700  UNION ALL
        SELECT 10,  7500  UNION ALL
        SELECT 11,  9600  UNION ALL
        SELECT 12, 12100  UNION ALL
        SELECT 13, 15000  UNION ALL
        SELECT 14, 18400  UNION ALL
        SELECT 15, 22300  UNION ALL
        SELECT 16, 26800  UNION ALL
        SELECT 17, 31900  UNION ALL
        SELECT 18, 37700  UNION ALL
        SELECT 19, 44200
    ) l
    CROSS JOIN (
        SELECT 0  AS pet_type UNION ALL SELECT 1  UNION ALL SELECT 2  UNION ALL SELECT 3  UNION ALL
        SELECT 4              UNION ALL SELECT 5  UNION ALL SELECT 6  UNION ALL SELECT 7  UNION ALL
        SELECT 8              UNION ALL SELECT 9  UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL
        SELECT 12             UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15 UNION ALL
        SELECT 16             UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL
        SELECT 20             UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL
        SELECT 24             UNION ALL SELECT 25 UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL
        SELECT 28
    ) t
) AS src
ON DUPLICATE KEY UPDATE
    experience_required = src.experience_required,
    energy_cap          = src.energy_cap,
    nutrition_cap       = src.nutrition_cap;
