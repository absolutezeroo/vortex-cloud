using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Habbicons;

/// <summary>
/// The player's whole Habbicon ownership, plus the ids they used most recently.
/// </summary>
/// <remarks>
/// The client replaces its owned dictionary wholesale from this, so it must carry everything the
/// player owns rather than a delta. It also diffs the new list against the old one to decide what
/// to flag as unseen, which is why a second identical send is harmless and a partial one is not.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record UserHabbiconsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<PlayerHabbiconSnapshot> Habbicons { get; init; }

    /// <summary>Most recently used first.</summary>
    [Id(1)]
    public required ImmutableArray<int> RecentHabbiconIds { get; init; }
}
