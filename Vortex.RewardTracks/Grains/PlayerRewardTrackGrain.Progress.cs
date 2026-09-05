using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.RewardTracks;
using Vortex.Primitives.Events;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.Protocol.Messages.Outgoing.RewardTracks;
using Vortex.RewardTracks.Progression;

namespace Vortex.RewardTracks.Grains;

/// <summary>
/// Turning gameplay signals into task progress, stage payments and track points.
/// </summary>
/// <remarks>
/// The arithmetic is not here — it is in <see cref="TaskProgressRules"/>, which is pure. This half
/// does the parts that need the world: which tracks are accepting progress right now, what is
/// stored, what gets written back, and who is told.
/// </remarks>
internal sealed partial class PlayerRewardTrackGrain
{
    public async Task ProgressAsync(
        string actionCode,
        int amount,
        string? target,
        CancellationToken ct
    )
    {
        if (!_featureEnabled || string.IsNullOrEmpty(actionCode))
        {
            return;
        }

        ImmutableArray<RewardTrackTaskRef> interested = catalog.TasksFor(actionCode);

        if (interested.IsDefaultOrEmpty)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        List<RewardTrackProgressNotice> notices = [];

        // One signal, every interested task. A "visit rooms" entry can advance the introduction
        // track and a summer campaign in the same turn, and neither knows about the other.
        foreach (RewardTrackTaskRef reference in interested)
        {
            if (
                !catalog.TryGetTrack(reference.TrackId, out RewardTrackDefinitionSnapshot? track)
                || !track.AcceptsProgressAt(now)
            )
            {
                continue;
            }

            RewardTrackTaskDefinitionSnapshot? task = FindTask(track, reference.TaskId);

            if (task is null)
            {
                continue;
            }

            RewardTrackProgressNotice? notice = await ApplyAsync(
                    track,
                    task,
                    outcome: null,
                    amount,
                    target,
                    now,
                    ct
                )
                .ConfigureAwait(true);

            if (notice is not null)
            {
                notices.Add(notice.Value);
            }
        }

        await NotifyAsync(notices, ct).ConfigureAwait(true);
    }

    public async Task ProgressTaskAsync(
        string trackId,
        string taskId,
        int amount,
        bool setExact,
        CancellationToken ct
    )
    {
        if (!_featureEnabled)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;

        if (
            !catalog.TryGetTrack(trackId, out RewardTrackDefinitionSnapshot? track)
            || !track.AcceptsProgressAt(now)
        )
        {
            return;
        }

        RewardTrackTaskDefinitionSnapshot? task = FindTask(track, taskId);

        if (task is null)
        {
            return;
        }

        TrackState state = EnsureTrack(trackId);
        TaskState taskState = EnsureTask(state, taskId);

        // The wired action addresses a track and a task by name, so it bypasses the action index
        // entirely -- it is not describing something the player did, it is a room saying what the
        // score now is.
        TaskProgressOutcome outcome = setExact
            ? TaskProgressRules.Set(
                task,
                taskState.HighestPaidLevelIndex,
                taskState.DistinctKeys,
                amount,
                state.PremiumUnlocked
            )
            : TaskProgressRules.Set(
                task,
                taskState.HighestPaidLevelIndex,
                taskState.DistinctKeys,
                taskState.ProgressCount + Math.Max(0, amount),
                state.PremiumUnlocked
            );

        RewardTrackProgressNotice? notice = await ApplyAsync(
                track,
                task,
                outcome,
                amount,
                target: null,
                now,
                ct
            )
            .ConfigureAwait(true);

        if (notice is not null)
        {
            await NotifyAsync([notice.Value], ct).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Applies one outcome to one task: writes the row, pays the stages, moves the points, and
    /// checks whether the track just finished.
    /// </summary>
    /// <param name="outcome">
    /// Already computed by the caller (the wired path), or null to compute it here from
    /// <paramref name="amount"/> and <paramref name="target"/>.
    /// </param>
    private async Task<RewardTrackProgressNotice?> ApplyAsync(
        RewardTrackDefinitionSnapshot track,
        RewardTrackTaskDefinitionSnapshot task,
        TaskProgressOutcome? outcome,
        int amount,
        string? target,
        DateTime now,
        CancellationToken ct
    )
    {
        TrackState state = EnsureTrack(track.TrackId);
        TaskState taskState = EnsureTask(state, task.TaskId);
        int previousProgress = taskState.ProgressCount;

        TaskProgressOutcome result =
            outcome
            ?? TaskProgressRules.Apply(
                task,
                taskState.ProgressCount,
                taskState.HighestPaidLevelIndex,
                taskState.DistinctKeys,
                amount,
                target,
                state.PremiumUnlocked
            );

        if (!result.Changed(previousProgress))
        {
            // Nothing moved. Not writing is the point: a distinct task on a room the player has
            // already visited fires this path on every entry, and persisting an unchanged row every
            // time is the difference between a quiet database and a hot one.
            return null;
        }

        // The boost applies to what is being granted now, and never to what was banked before
        // premium was bought. That is the whole of the no-retroactive-XP rule.
        int points = PremiumBoost.Apply(result.PointsGranted, track.Premium, state.PremiumUnlocked);

        taskState.ProgressCount = result.NewProgress;
        taskState.HighestPaidLevelIndex = result.HighestPaidLevelIndex;
        taskState.DistinctKeys = result.DistinctKeys;
        state.Points += points;
        state.ContentVersion = track.ContentVersion;

        await PersistProgressAsync(track.TrackId, task.TaskId, state, taskState, ct)
            .ConfigureAwait(true);

        foreach (int levelIndex in result.StagesPaid)
        {
            await events
                .PublishAsync(
                    new RewardTrackStageCompletedEvent(
                        PlayerId,
                        track.TrackId,
                        task.TaskId,
                        levelIndex,
                        points
                    ),
                    ct
                )
                .ConfigureAwait(true);

            logger.LogInformation(
                "Player {PlayerId} completed stage {LevelIndex} of {TrackId}/{TaskId}: {Progress} -> {NewProgress}, +{Points} point(s), total {Total}.",
                PlayerId,
                levelIndex,
                track.TrackId,
                task.TaskId,
                previousProgress,
                result.NewProgress,
                points,
                state.Points
            );
        }

        if (points > 0)
        {
            await CheckCompletionAsync(track, state, now, ct).ConfigureAwait(true);
        }

        return new RewardTrackProgressNotice
        {
            TrackId = track.TrackId,
            TaskId = task.TaskId,
            ProgressCount = result.NewProgress,
            Points = state.Points,
        };
    }

    /// <summary>
    /// Writes the track row and the task row. Upserted in one commit, so a stage that paid points
    /// can never be persisted without the progress that earned it.
    /// </summary>
    private async Task PersistProgressAsync(
        string trackId,
        string taskId,
        TrackState state,
        TaskState taskState,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        PlayerRewardTrackEntity? trackRow = await db
            .PlayerRewardTracks.FirstOrDefaultAsync(
                t => t.PlayerEntityId == PlayerId && t.TrackId == trackId,
                ct
            )
            .ConfigureAwait(true);

        if (trackRow is null)
        {
            db.PlayerRewardTracks.Add(
                new PlayerRewardTrackEntity
                {
                    PlayerEntityId = PlayerId,
                    TrackId = trackId,
                    Points = state.Points,
                    PremiumUnlocked = state.PremiumUnlocked,
                    PremiumUnlockedAt = state.PremiumUnlockedAt,
                    CompletedAt = state.CompletedAt,
                    ContentVersion = state.ContentVersion,
                }
            );
        }
        else
        {
            trackRow.Points = state.Points;
            trackRow.ContentVersion = state.ContentVersion;
        }

        PlayerRewardTrackTaskEntity? taskRow = await db
            .PlayerRewardTrackTasks.FirstOrDefaultAsync(
                t => t.PlayerEntityId == PlayerId && t.TrackId == trackId && t.TaskId == taskId,
                ct
            )
            .ConfigureAwait(true);

        if (taskRow is null)
        {
            db.PlayerRewardTrackTasks.Add(
                new PlayerRewardTrackTaskEntity
                {
                    PlayerEntityId = PlayerId,
                    TrackId = trackId,
                    TaskId = taskId,
                    ProgressCount = taskState.ProgressCount,
                    HighestPaidLevelIndex = taskState.HighestPaidLevelIndex,
                    DistinctKeys = taskState.DistinctKeys,
                }
            );
        }
        else
        {
            taskRow.ProgressCount = taskState.ProgressCount;
            taskRow.HighestPaidLevelIndex = taskState.HighestPaidLevelIndex;
            taskRow.DistinctKeys = taskState.DistinctKeys;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(true);
    }

    /// <summary>
    /// Records the completion transition once, if the track's policy is now met.
    /// </summary>
    /// <remarks>
    /// <c>CompletedAt</c> is the guard: a track completes once and stays completed, so the event a
    /// follow-on chapter unlocks from cannot fire twice, and a later content edit that adds a prize
    /// does not un-finish somebody's campaign.
    /// </remarks>
    private async Task CheckCompletionAsync(
        RewardTrackDefinitionSnapshot track,
        TrackState state,
        DateTime now,
        CancellationToken ct
    )
    {
        if (state.CompletedAt is not null)
        {
            return;
        }

        PlayerRewardTrackStateSnapshot snapshot = ToSnapshot(track.TrackId, state);
        RewardTrackViewSnapshot view = TrackViewBuilder.Build(track, snapshot);

        if (!TrackGatingRules.IsComplete(track, view, snapshot))
        {
            return;
        }

        state.CompletedAt = now;

        await using (
            VortexDbContext db = await dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true)
        )
        {
            await db
                .PlayerRewardTracks.Where(t =>
                    t.PlayerEntityId == PlayerId && t.TrackId == track.TrackId
                )
                .ExecuteUpdateAsync(up => up.SetProperty(t => t.CompletedAt, now), ct)
                .ConfigureAwait(true);
        }

        await events
            .PublishAsync(
                new RewardTrackCompletedEvent(PlayerId, track.TrackId, track.CompletionPolicy),
                ct
            )
            .ConfigureAwait(true);

        logger.LogInformation(
            "Player {PlayerId} completed reward track {TrackId} ({Policy}).",
            PlayerId,
            track.TrackId,
            track.CompletionPolicy
        );

        // A chapter finishing may have unlocked the next one, which the client has never been sent.
        await PushTracksAsync(reload: false, ct).ConfigureAwait(true);
    }

    /// <summary>
    /// Sends one incremental update per task that moved. The client patches four fields in place
    /// rather than redrawing the track, which is why progress does not push the whole list.
    /// </summary>
    private async Task NotifyAsync(
        IReadOnlyList<RewardTrackProgressNotice> notices,
        CancellationToken ct
    )
    {
        foreach (RewardTrackProgressNotice notice in notices)
        {
            await Presence
                .SendComposerAsync(
                    new RewardTrackProgressMessageComposer
                    {
                        TrackId = notice.TrackId,
                        TaskId = notice.TaskId,
                        ProgressCount = notice.ProgressCount,
                        Points = notice.Points,
                    }
                )
                .ConfigureAwait(true);
        }
    }

    private static RewardTrackTaskDefinitionSnapshot? FindTask(
        RewardTrackDefinitionSnapshot track,
        string taskId
    )
    {
        foreach (RewardTrackTaskDefinitionSnapshot task in track.Tasks)
        {
            if (string.Equals(task.TaskId, taskId, StringComparison.Ordinal))
            {
                return task;
            }
        }

        return null;
    }

    private static TaskState EnsureTask(TrackState state, string taskId)
    {
        if (!state.Tasks.TryGetValue(taskId, out TaskState? task))
        {
            task = new TaskState();
            state.Tasks[taskId] = task;
        }

        return task;
    }
}
