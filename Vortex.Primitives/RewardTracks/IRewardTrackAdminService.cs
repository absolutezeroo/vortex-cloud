using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.RewardTracks.Admin;

namespace Vortex.Primitives.RewardTracks;

/// <summary>
/// The management surface for reward-track content and for one player's progress.
/// </summary>
/// <remarks>
/// <para>
/// UI-agnostic: specs and results, never HTTP. The dashboard is one caller and is not named here.
/// </para>
/// <para>
/// Every structural write bumps the track's content version and reloads the catalog, which is what
/// makes an edit reach a player who is already looking at the track — the client is pushed the list
/// again with its own reload flag, and shows the "reward track updated" line it has for exactly
/// this.
/// </para>
/// </remarks>
public interface IRewardTrackAdminService
{
    Task<RewardTrackAdminResult> CreateTrackAsync(RewardTrackSpec spec, CancellationToken ct);

    Task<RewardTrackAdminResult> UpdateTrackAsync(
        int trackRowId,
        RewardTrackSpec spec,
        CancellationToken ct
    );

    /// <summary>
    /// Copies a track, its tasks and its prizes under a new content id, as a draft. How a seasonal
    /// campaign becomes next season's without retyping it.
    /// </summary>
    Task<RewardTrackAdminResult> CloneTrackAsync(
        int trackRowId,
        string newTrackId,
        CancellationToken ct
    );

    /// <summary>
    /// Publishes a draft. Refuses a track the validator reports problems on — a published track
    /// with an unreachable milestone is worse than an unpublished one.
    /// </summary>
    Task<RewardTrackAdminResult> PublishTrackAsync(int trackRowId, CancellationToken ct);

    /// <summary>Takes a track out of service without deleting it or anybody's progress.</summary>
    Task<RewardTrackAdminResult> ArchiveTrackAsync(int trackRowId, CancellationToken ct);

    /// <summary>
    /// Deletes a track and its content. Refuses while any player has progress on it: their rows key
    /// on the content id and would outlive the definition.
    /// </summary>
    Task<RewardTrackAdminResult> DeleteTrackAsync(int trackRowId, CancellationToken ct);

    Task<RewardTrackAdminResult> UpsertTaskAsync(
        int trackRowId,
        RewardTrackTaskSpec spec,
        CancellationToken ct
    );

    Task<RewardTrackAdminResult> DeleteTaskAsync(int taskRowId, CancellationToken ct);

    Task<RewardTrackAdminResult> UpsertPrizeAsync(
        int trackRowId,
        RewardTrackPrizeSpec spec,
        CancellationToken ct
    );

    Task<RewardTrackAdminResult> DeletePrizeAsync(int prizeRowId, CancellationToken ct);

    /// <summary>Participation and conversion counts per track.</summary>
    Task<IReadOnlyList<RewardTrackStats>> GetStatsAsync(CancellationToken ct);

    /// <summary>One player's standing on every track they have touched.</summary>
    Task<IReadOnlyList<PlayerRewardTrackAdminRow>> GetPlayerProgressAsync(
        int playerId,
        CancellationToken ct
    );

    /// <summary>Wipes one player's progress on one track, claims included.</summary>
    Task<RewardTrackAdminResult> ResetPlayerTrackAsync(
        int playerId,
        string trackId,
        CancellationToken ct
    );

    /// <summary>Turns premium on for a player without charging them.</summary>
    Task<RewardTrackAdminResult> GrantPremiumAsync(
        int playerId,
        string trackId,
        CancellationToken ct
    );

    /// <summary>
    /// Checks every track for content problems. Reports them all rather than failing on the first.
    /// </summary>
    Task<RewardTrackContentReport> ValidateAsync(CancellationToken ct);
}
