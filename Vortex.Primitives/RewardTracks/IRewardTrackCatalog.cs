using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.Primitives.RewardTracks;

/// <summary>
/// Every reward track definition, cached in process, plus the index that makes progression cheap.
/// </summary>
/// <remarks>
/// <para>
/// The index is the point. Without it, every gameplay event would have to reach a grain and scan
/// every task of every campaign to discover it was of no interest — and the whole design is that
/// dozens of campaigns can coexist. <see cref="IsActionInteresting"/> answers from a hash set in
/// the calling thread, so an event no content cares about costs nothing at all.
/// </para>
/// <para>
/// Reloaded by <c>IRewardTrackAdminService</c> after a content write, never mutated by gameplay.
/// </para>
/// </remarks>
public interface IRewardTrackCatalog
{
    /// <summary>
    /// The name this cache answers to in <see cref="Hosting.IReferenceDataReloader"/>.
    /// </summary>
    /// <remarks>
    /// The admin service reloads through the reloader rather than by holding the concrete catalogue,
    /// so that it does not need a reference to the assembly that implements it. The reloader names
    /// caches by their implementing type's name; a test pins that this string is still that name,
    /// because the mismatch would otherwise only show as a reload that quietly did nothing.
    /// </remarks>
    public const string CacheName = "RewardTrackCatalog";

    /// <summary>Every track, published or not, in display order.</summary>
    ImmutableArray<RewardTrackDefinitionSnapshot> Tracks { get; }

    bool TryGetTrack(string trackId, [NotNullWhen(true)] out RewardTrackDefinitionSnapshot? track);

    /// <summary>
    /// Whether any task on any track is defined on this action code. The one question the event
    /// bridge asks, and it must be answerable without a grain call.
    /// </summary>
    bool IsActionInteresting(string actionCode);

    /// <summary>
    /// The (track, task) pairs an action code feeds, across every track. Tracks that are not
    /// accepting progress are still listed — the caller knows the clock, this only knows content.
    /// </summary>
    ImmutableArray<RewardTrackTaskRef> TasksFor(string actionCode);

    /// <summary>
    /// Every action any content is defined on, for the signal interest gate.
    /// </summary>
    /// <remarks>
    /// The same set <see cref="IsActionInteresting"/> answers from, exposed whole because the gate
    /// asks about actions this catalogue has never heard of and must answer without allocating.
    /// Read live rather than copied: a content write swaps the index, and the next event sees it.
    /// </remarks>
    ImmutableHashSet<string> Actions { get; }
}

/// <summary>A task addressed by its track. What the action index holds.</summary>
public readonly record struct RewardTrackTaskRef(string TrackId, string TaskId);
