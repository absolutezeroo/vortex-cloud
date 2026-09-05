using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.Primitives.RewardTracks.Grains;

/// <summary>
/// One player's reward-track state, across every track at once. The single mutation boundary for
/// points, task progress, claims and premium: Orleans runs one turn at a time per player, so the
/// read-calculate-write that every one of those is stays atomic without a lock anywhere.
/// </summary>
/// <remarks>
/// There is deliberately no notion of a "current" track here. The client picks one to look at; the
/// server progresses all of them, and one signal can advance a task on several.
/// </remarks>
public interface IPlayerRewardTrackGrain : IGrainWithIntegerKey
{
    /// <summary>
    /// Every track this player can see, resolved against their state. Used by the login push and by
    /// anything that needs to redraw the whole list.
    /// </summary>
    public Task<ImmutableArray<RewardTrackViewSnapshot>> GetTracksAsync(CancellationToken ct);

    /// <summary>
    /// Pushes the whole track list to the player.
    /// </summary>
    /// <param name="reload">
    /// Sets the client's own reload flag, which makes it drop its cached views and tell the player
    /// the tracks changed underneath them. Passed true after a content edit, never after ordinary
    /// progress.
    /// </param>
    public Task PushTracksAsync(bool reload, CancellationToken ct);

    /// <summary>
    /// Advances every task defined on <paramref name="actionCode"/>, across every track that is
    /// accepting progress.
    /// </summary>
    /// <param name="amount">
    /// How much to add, for counter and amount tasks; the reported total for absolute ones. Always
    /// at least 1 for a signal that happened.
    /// </param>
    /// <param name="target">
    /// What the signal was about — a room id, a furniture class, a Habbicon id. A task with a
    /// <c>Parameter</c> only advances when it matches; a task without one ignores it. Also the
    /// dedup key for distinct-mode tasks.
    /// </param>
    /// <param name="facts">
    /// The named facts about what happened, from <see cref="RewardTrackFacts"/>. This is what makes
    /// a sequence composable: a step filters on them, and a later step can point back at what an
    /// earlier one matched. <c>target</c> is one of them, which is why a task's <c>Parameter</c>
    /// keeps working unchanged.
    /// </param>
    public Task ProgressAsync(
        string actionCode,
        int amount,
        string? target,
        ImmutableArray<RewardTrackFactSnapshot> facts,
        CancellationToken ct
    );

    /// <summary>
    /// Advances one named task directly, bypassing the action index. The wired
    /// <c>PROGRESS_REWARD_TRACK</c> action's entry point: it names a track and a task rather than
    /// describing something the player did.
    /// </summary>
    /// <param name="setExact">
    /// True writes <paramref name="amount"/> as the progress; false adds it. The wired action's
    /// "add to existing score" checkbox, inverted.
    /// </param>
    public Task ProgressTaskAsync(
        string trackId,
        string taskId,
        int amount,
        bool setExact,
        CancellationToken ct
    );

    /// <summary>
    /// Claims one prize. Validates the track, the prize, the points, premium and the claim window
    /// server-side — the client sends two strings and none of them is trusted. Granting the bundle
    /// and recording the claim commit together, so a retry of a claim that already landed reports
    /// <see cref="RewardClaimResult.AlreadyClaimed"/> rather than paying twice.
    /// </summary>
    public Task<RewardClaimOutcome> ClaimPrizeAsync(
        string trackId,
        string prizeId,
        CancellationToken ct
    );

    /// <summary>
    /// Claims every prize currently claimable on a track, lowest requirement first. Each prize is
    /// its own transaction: one that fails leaves the ones before it granted and stops, which is the
    /// only semantics that never loses a reward and never hands one out twice.
    /// </summary>
    public Task<ImmutableArray<RewardClaimOutcome>> ClaimAllAsync(
        string trackId,
        CancellationToken ct
    );

    /// <summary>
    /// Buys premium on one track. Debits every configured currency together or not at all, credits
    /// the instant points, and makes the premium prizes the player already has the points for
    /// claimable.
    /// </summary>
    public Task<RewardPremiumOutcome> PurchasePremiumAsync(string trackId, CancellationToken ct);

    /// <summary>Grants premium without payment. Operator-only.</summary>
    public Task<bool> GrantPremiumAsync(string trackId, CancellationToken ct);

    /// <summary>
    /// Whether anything is waiting to be claimed anywhere. Answers the red dot without the caller
    /// resolving every track.
    /// </summary>
    public Task<bool> HasUnclaimedRewardsAsync(CancellationToken ct);

    /// <summary>Raw stored state, for the dashboard's player-progression inspector.</summary>
    public Task<ImmutableArray<PlayerRewardTrackStateSnapshot>> GetRawStateAsync(
        CancellationToken ct
    );

    /// <summary>Wipes the player's progress on one track. Operator-only.</summary>
    public Task<bool> ResetTrackAsync(string trackId, CancellationToken ct);

    /// <summary>
    /// Drops the grain's cached view of the definitions. Called by the admin service after a content
    /// write, so a player who is online sees the edit without reconnecting.
    /// </summary>
    public Task InvalidateAsync(CancellationToken ct);
}
