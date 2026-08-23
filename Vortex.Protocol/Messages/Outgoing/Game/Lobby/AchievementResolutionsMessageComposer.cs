using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Game.Lobby;

/// <summary>
/// The picker dialog: everything this statue offers, and how long is left to finish.
///
/// An empty list is not an empty dialog — the client returns early and never shows the window at
/// all, which is the right answer for a statue whose campaign has no achievements configured.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record AchievementResolutionsMessageComposer : IComposer
{
    [Id(0)]
    public required int StuffId { get; init; }

    [Id(1)]
    public required ImmutableArray<AchievementResolutionSnapshot> Achievements { get; init; }

    /// <summary>Seconds left, not a timestamp: the client feeds it straight to a countdown widget.</summary>
    [Id(2)]
    public required int SecondsLeft { get; init; }
}
