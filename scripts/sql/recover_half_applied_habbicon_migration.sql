-- Recovery for a half-applied AddHabbiconsAndRewardTracks.
--
-- WHEN YOU NEED THIS: the first version of that migration created its tables and then failed on
--
--     CREATE UNIQUE INDEX IX_player_reward_track_claims_player_id_track_id_prize_id
--     -> Specified key was too long; max key length is 3072 bytes
--
-- because `track_id` and `prize_id` were unbounded strings, which Pomelo maps to varchar(512) in
-- utf8mb4 -- 2048 bytes each, so the three-column index came to just over 4KB. The entities now cap
-- those columns at 64 characters (VortexEntity.ContentIdLength) and the regenerated migration
-- creates them as varchar(64).
--
-- WHY THE DATABASE IS STUCK: MySQL does not roll back DDL. Every statement before the failing index
-- committed -- the eleven tables and the `messenger_messages.habbicon_id` column are all there --
-- but the row in `__EFMigrationsHistory` is only written after the whole migration succeeds, so EF
-- believes none of it ran and replays from the top. The second attempt therefore dies on
-- `Duplicate column name 'habbicon_id'`.
--
-- WHAT THIS DOES: removes exactly what the failed run left behind, so the corrected migration can
-- apply from a clean slate. Safe because every one of these objects was created by that failed run
-- moments ago and cannot contain anything: no code had started writing to them.
--
-- DO NOT run this on a hotel where the migration has already applied successfully -- it would drop
-- live Habbicon ownership and reward-track progress. Step 1 tells you which case you are in.

-- ---------------------------------------------------------------------------------------------
-- 1. DRY RUN. Read both answers before running anything below.
-- ---------------------------------------------------------------------------------------------

-- (a) Did the migration ever complete? Expect ZERO rows. If it returns a row, STOP: the schema is
--     correct and applied, and this script would destroy real data.
-- The column is `MigrationId`, EF's own casing, not snake_case like the rest of this schema.
SELECT MigrationId
FROM `__EFMigrationsHistory`
WHERE MigrationId LIKE '%AddHabbiconsAndRewardTracks%'
   OR MigrationId LIKE '%SeedHabbiconsAndIntroductionTrack%';

-- (b) What did the failed run leave? Expect up to 11 tables, all empty. A non-zero count anywhere
--     means somebody wrote to them after all -- stop and work out how.
SELECT
    t.table_name,
    t.table_rows AS approx_rows
FROM information_schema.tables t
WHERE t.table_schema = DATABASE()
  AND t.table_name IN (
      'player_reward_track_claims',
      'player_reward_track_tasks',
      'player_reward_tracks',
      'reward_track_prize_rewards',
      'reward_track_prizes',
      'reward_track_task_levels',
      'reward_track_tasks',
      'reward_tracks',
      'player_habbicons',
      'habbicons',
      'habbicon_collections'
  )
ORDER BY t.table_name;

-- ---------------------------------------------------------------------------------------------
-- 2. THE WAY BACK. There is none: these tables hold nothing. That is the whole reason dropping
--    them is the right move rather than patching the schema in place -- there is no state to
--    preserve, so the clean slate costs nothing and leaves no half-corrected columns behind.
-- ---------------------------------------------------------------------------------------------

-- ---------------------------------------------------------------------------------------------
-- 3. THE FIX. Children before parents: the foreign keys are real.
-- ---------------------------------------------------------------------------------------------

DROP TABLE IF EXISTS `player_reward_track_claims`;
DROP TABLE IF EXISTS `player_reward_track_tasks`;
DROP TABLE IF EXISTS `player_reward_tracks`;
DROP TABLE IF EXISTS `reward_track_prize_rewards`;
DROP TABLE IF EXISTS `reward_track_prizes`;
DROP TABLE IF EXISTS `reward_track_task_levels`;
DROP TABLE IF EXISTS `reward_track_tasks`;
DROP TABLE IF EXISTS `reward_tracks`;
DROP TABLE IF EXISTS `player_habbicons`;
DROP TABLE IF EXISTS `habbicons`;
DROP TABLE IF EXISTS `habbicon_collections`;

-- The one column the failed run added outside its own tables. Guarded so this script is safe to run
-- twice: MySQL has no `DROP COLUMN IF EXISTS`, and an unguarded drop fails on the second pass.
SET @drop_habbicon_column := (
    SELECT IF(
        EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'messenger_messages'
              AND column_name = 'habbicon_id'
        ),
        'ALTER TABLE `messenger_messages` DROP COLUMN `habbicon_id`',
        'SELECT ''messenger_messages.habbicon_id is already absent'''
    )
);

PREPARE stmt FROM @drop_habbicon_column;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------------------------
-- 4. THEN. Re-run the migrations; both now apply cleanly:
--
--        cd Vortex.Database
--        Vortex__Database__ServerVersion=8.0.32-mysql dotnet ef database update
--
--    Or just restart Vortex.Main if it migrates on boot.
-- ---------------------------------------------------------------------------------------------
