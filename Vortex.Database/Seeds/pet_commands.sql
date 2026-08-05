-- Pet commands, rebuilt 2026-08-03 on the client's own command ids.
--
-- The previous seed numbered its rows 0=Sit, 1=Stand, 2=Lay Down ... while pet_command_names is
-- lifted verbatim from the client's text bundle, where 0=Free, 1=Sit, 2=Down, 8=Stand. Both tables
-- are read with the same id -- PetCommandProvider resolves the spoken word to a name id and then
-- looks up the config by that id -- so the two schemes collided: telling a pet to Sit made it
-- stand, Free made it sit, and Nest matched no row at all and was silently ignored.
--
-- Postures are restricted to the ids a pet asset actually declares. Decoding dog.nitro gives
-- std, beg, bnd, ded, eat, jmp, lay, pla, rdy, scr, sit, snf, spk, mv -- the previous seed used
-- `rll` for Roll Over and `flp` for Back Flip, which resolve to nothing, so the client fell back to
-- standing and the trick was invisible.
--
-- Only commands whose behaviour the room can actually carry out are seeded. An unseeded command
-- resolves to nothing and the pet answers UNKNOWN_COMMAND, which is honest; a seeded command that
-- does the wrong thing is not. Follow, Stay, Silent and the rest wait for the behaviour to exist.
--
-- Re-running is harmless: the delete scopes to the rows this file owns.

DELETE FROM `pet_commands`;

INSERT INTO `pet_commands`
    (`pet_type`, `command`, `level_required`, `posture`, `energy_cost`, `xp_reward`, `created_at`, `updated_at`)
SELECT t.pet_type, c.command, c.level_required, c.posture, c.energy_cost, c.xp_reward, NOW(), NOW()
FROM (
    -- command, level, posture, energy, xp
    SELECT  0 AS command, 0 AS level_required, ''    AS posture, 0 AS energy_cost, 1 AS xp_reward UNION ALL -- Free
    SELECT  1, 0, 'sit',  5, 3 UNION ALL -- Sit
    SELECT  2, 0, 'lay',  5, 3 UNION ALL -- Down
    SELECT  4, 1, 'beg',  5, 3 UNION ALL -- Beg
    SELECT  5, 4, 'ded', 10, 5 UNION ALL -- Play dead
    SELECT  8, 0, 'std',  5, 3 UNION ALL -- Stand
    SELECT  9, 3, 'jmp', 10, 5 UNION ALL -- Jump
    SELECT 10, 2, 'spk',  5, 3 UNION ALL -- Speak
    SELECT 11, 3, 'pla', 10, 5 UNION ALL -- Play
    SELECT 13, 2, 'lay',  0, 1 UNION ALL -- Nest: the room walks it to its nest
    SELECT 43, 2, 'eat',  0, 1           -- Eat: the room walks it to a bowl
) c
CROSS JOIN (
    -- Every pet type the catalogue sells, minus the monsterplant, which is rooted and obeys nothing.
    SELECT  0 AS pet_type UNION ALL SELECT  1 UNION ALL SELECT  2 UNION ALL SELECT  3 UNION ALL
    SELECT  4 UNION ALL SELECT  5 UNION ALL SELECT  6 UNION ALL SELECT  7 UNION ALL
    SELECT  8 UNION ALL SELECT  9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL
    SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15 UNION ALL
    SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20 UNION ALL
    SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24 UNION ALL
    SELECT 25 UNION ALL SELECT 28 UNION ALL SELECT 29 UNION ALL SELECT 30 UNION ALL
    SELECT 31 UNION ALL SELECT 32 UNION ALL SELECT 35 UNION ALL SELECT 36
) t;

-- ─── Pet toys the catalogue left on `default` ────────────────────────────────
-- Habbo's guides name toys as what cheers a pet up. None of these carried a logic, so no pet has
-- ever been able to play with one. Arcturus calls them pet_toy and pet_trampoline.
UPDATE `furniture_definitions`
SET `logic` = 'pet_toy'
WHERE `name` IN ('pet_toy_ball', 'pet_ufo_toy', 'pet_puppy_toy');

UPDATE `furniture_definitions`
SET `logic` = 'pet_trampoline'
WHERE `name` IN ('pet_toy_trampoline');
