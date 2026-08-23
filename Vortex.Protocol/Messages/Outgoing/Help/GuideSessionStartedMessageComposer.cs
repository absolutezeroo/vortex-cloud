using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Help;

/// <summary>
/// The pair is made. Both sides get the same packet and each reads the other out of it, which is why
/// it names both people rather than "the partner".
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuideSessionStartedMessageComposer : IComposer
{
    [Id(0)]
    public required int RequesterId { get; init; }

    [Id(1)]
    public required string RequesterName { get; init; }

    [Id(2)]
    public required string RequesterFigure { get; init; }

    [Id(3)]
    public required int GuideId { get; init; }

    [Id(4)]
    public required string GuideName { get; init; }

    [Id(5)]
    public required string GuideFigure { get; init; }
}
