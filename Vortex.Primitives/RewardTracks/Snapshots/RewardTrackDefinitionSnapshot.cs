using System;
using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>A whole reward track, as content. No player is involved.</summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackDefinitionSnapshot
{
    /// <summary>
    /// The client-facing track id (<c>introduction</c>). Localization stem, deep-link target
    /// (<c>reward_track/open/&lt;id&gt;</c>) and the identity every player row hangs off.
    /// </summary>
    [Id(0)]
    public required string TrackId { get; init; }

    /// <summary>
    /// One of the client's five palettes: <c>blue</c> (default), <c>orange</c>,
    /// <c>forest_green</c>, <c>red</c>, <c>cyan</c>. Anything else renders blue.
    /// </summary>
    [Id(1)]
    public required string Theme { get; init; }

    [Id(2)]
    public required RewardTrackStatus Status { get; init; }

    [Id(3)]
    public required int SortOrder { get; init; }

    /// <summary>When the track starts being served. Null = as soon as it is published.</summary>
    [Id(4)]
    public DateTime? StartsAtUtc { get; init; }

    /// <summary>When tasks stop advancing. Null = never.</summary>
    [Id(5)]
    public DateTime? ProgressEndsAtUtc { get; init; }

    /// <summary>
    /// When claiming closes, and with it the whole track. Null = never. Must not be earlier than
    /// <see cref="ProgressEndsAtUtc"/>; the validator rejects that, since it would strand points
    /// the player can never spend.
    /// </summary>
    [Id(6)]
    public DateTime? ClaimEndsAtUtc { get; init; }

    [Id(7)]
    public required RewardTrackUnlockKind UnlockKind { get; init; }

    [Id(8)]
    public required string UnlockValue { get; init; }

    [Id(9)]
    public required RewardTrackCompletionPolicy CompletionPolicy { get; init; }

    /// <summary>Premium tier, or null when the track has none.</summary>
    [Id(10)]
    public RewardTrackPremiumSnapshot? Premium { get; init; }

    [Id(11)]
    public required ImmutableArray<RewardTrackTaskDefinitionSnapshot> Tasks { get; init; }

    /// <summary>Milestones in ascending <see cref="RewardTrackPrizeDefinitionSnapshot.RequiredPoints"/> order.</summary>
    [Id(12)]
    public required ImmutableArray<RewardTrackPrizeDefinitionSnapshot> Prizes { get; init; }

    /// <summary>
    /// Bumped by the admin layer whenever the track's structure changes. A player holding an older
    /// version is pushed a fresh list with the client's own <c>reload</c> flag set, which is exactly
    /// what that flag is for ("Some changes to the reward tracks were made behind the scenes").
    /// </summary>
    [Id(13)]
    public required int ContentVersion { get; init; }

    /// <summary>Hidden tracks are served only to a player who already has a row on them.</summary>
    [Id(14)]
    public required bool Hidden { get; init; }

    /// <summary>Free-form campaign tag, so chapters of one campaign can find each other.</summary>
    [Id(15)]
    public string CampaignCode { get; init; } = string.Empty;

    /// <summary>Whether tasks still advance at <paramref name="nowUtc"/>.</summary>
    public bool AcceptsProgressAt(DateTime nowUtc) =>
        Status == RewardTrackStatus.Active
        && (StartsAtUtc is null || StartsAtUtc <= nowUtc)
        && (ProgressEndsAtUtc is null || ProgressEndsAtUtc > nowUtc);

    /// <summary>Whether prizes can still be claimed at <paramref name="nowUtc"/>.</summary>
    public bool AcceptsClaimsAt(DateTime nowUtc) =>
        Status is RewardTrackStatus.Active or RewardTrackStatus.Ended
        && (StartsAtUtc is null || StartsAtUtc <= nowUtc)
        && (ClaimEndsAtUtc is null || ClaimEndsAtUtc > nowUtc);

    /// <summary>Whether the track is served to the client at all at <paramref name="nowUtc"/>.</summary>
    public bool IsVisibleAt(DateTime nowUtc) => AcceptsClaimsAt(nowUtc);
}
