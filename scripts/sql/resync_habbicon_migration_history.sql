-- Resync `__EFMigrationsHistory` after the habbicon recovery script ran on an applied database.
--
-- WHEN YOU NEED THIS: `dotnet ef database update` (or a Vortex.Main boot) dies on
--
--     Applying migration '20260905170000_AlignHabbiconIdsToAssetPack'.
--     Table 'turbo.habbicons' doesn't exist
--
-- while `__EFMigrationsHistory` happily lists 20260905144155_AddHabbiconsAndRewardTracks.
--
-- WHY THE DATABASE IS STUCK: `recover_half_applied_habbicon_migration.sql` drops the eleven
-- habbicon/reward-track tables and `messenger_messages.habbicon_id`. Its step 1(a) is a SELECT the
-- operator is meant to read, not a statement that stops the script, so running it on a hotel where
-- that migration had already completed removes the schema and leaves the three history rows behind.
-- EF then skips exactly the migrations that would rebuild the schema and applies the later ones,
-- which reference tables that are no longer there.
--
-- WHAT THIS DOES: deletes those three history rows so EF replays create + seed + action-code fix.
-- It refuses to run while `habbicons` exists -- in that case the history is correct and the failure
-- has a different cause.
--
-- DATA LOSS: none from this script. Whatever those tables held was destroyed by the drop already;
-- deleting the history rows is what lets the migrations put an empty, correct schema back.

-- ---------------------------------------------------------------------------------------------
-- 1. DRY RUN. Expect the three history rows, and zero tables.
-- ---------------------------------------------------------------------------------------------

-- The column is `MigrationId`, EF's own casing, not snake_case like the rest of this schema.
SELECT MigrationId
FROM `__EFMigrationsHistory`
WHERE MigrationId IN (
    '20260905144155_AddHabbiconsAndRewardTracks',
    '20260905144311_SeedHabbiconsAndIntroductionTrack',
    '20260905160000_FixRewardTrackActionCodes'
);

SELECT table_name
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN (
      'player_reward_track_claims', 'player_reward_track_tasks', 'player_reward_tracks',
      'reward_track_prize_rewards', 'reward_track_prizes', 'reward_track_task_levels',
      'reward_track_tasks', 'reward_tracks', 'player_habbicons', 'habbicons',
      'habbicon_collections'
  );

-- ---------------------------------------------------------------------------------------------
-- 2. THE WAY BACK. Re-insert the rows this deletes, with any `ProductVersion` from a neighbouring
--    row. Only useful if you decide the schema was there after all -- check `habbicons` first.
-- ---------------------------------------------------------------------------------------------

-- ---------------------------------------------------------------------------------------------
-- 3. THE FIX. The guard aborts on a missing sentinel table rather than deleting anything, because
--    a `SELECT` nobody reads is how the database got into this state in the first place.
-- ---------------------------------------------------------------------------------------------

SET @habbicons_present := (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'habbicons'
);

SET @guard := IF(
    @habbicons_present > 0,
    'SELECT * FROM `STOP_habbicons_exists_the_history_rows_are_correct`',
    'SELECT ''habbicons is absent -- safe to replay the three migrations'' AS guard'
);

PREPARE guard FROM @guard;
EXECUTE guard;
DEALLOCATE PREPARE guard;

DELETE FROM `__EFMigrationsHistory`
WHERE MigrationId IN (
    '20260905144155_AddHabbiconsAndRewardTracks',
    '20260905144311_SeedHabbiconsAndIntroductionTrack',
    '20260905160000_FixRewardTrackActionCodes'
);

-- ---------------------------------------------------------------------------------------------
-- 4. THEN. Re-run the migrations. EF applies pending ids in order regardless of what is already
--    applied, so the three replay ahead of 20260905170000_AlignHabbiconIdsToAssetPack:
--
--        cd Vortex.Database
--        Vortex__Database__ServerVersion=8.0.32-mysql dotnet ef database update
--
--    Or just restart Vortex.Main if it migrates on boot.
-- ---------------------------------------------------------------------------------------------
