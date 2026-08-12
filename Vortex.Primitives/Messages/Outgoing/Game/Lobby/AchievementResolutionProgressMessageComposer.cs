using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Game.Lobby;

/// <summary>
/// The progress view for a challenge already under way on this statue.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record AchievementResolutionProgressMessageComposer : IComposer
{
    [Id(0)]
    public required int StuffId { get; init; }

    [Id(1)]
    public required int AchievementId { get; init; }

    /// <summary>Badge of the level being aimed at. The dialog draws it and reads its name and
    /// description from the badge, so an unknown code shows an empty card rather than an error.</summary>
    [Id(2)]
    public required string RequiredLevelBadgeCode { get; init; }

    [Id(3)]
    public required int UserProgress { get; init; }

    [Id(4)]
    public required int TotalProgress { get; init; }

    /// <summary>Seconds left, same countdown widget as the picker.</summary>
    [Id(5)]
    public required int SecondsLeft { get; init; }
}
