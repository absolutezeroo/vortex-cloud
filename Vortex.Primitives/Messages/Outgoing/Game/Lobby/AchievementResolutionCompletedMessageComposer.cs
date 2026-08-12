using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Game.Lobby;

/// <summary>
/// The congratulations screen. Note the order: the wire carries the stuff code first and the badge
/// second, even though the client's own handler passes them the other way round when it calls the
/// view. Following the call site rather than the parser writes them backwards.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record AchievementResolutionCompletedMessageComposer : IComposer
{
    /// <summary>Class name of the furni awarded for finishing — the statue itself.</summary>
    [Id(0)]
    public required string StuffCode { get; init; }

    [Id(1)]
    public required string BadgeCode { get; init; }
}
