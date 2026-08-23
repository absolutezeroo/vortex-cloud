using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// Where the person who asked for help currently is. The guide's client walks itself there on
/// receipt, so a zero is the only way to say "they are nowhere I can send you".
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuideSessionRequesterRoomMessageComposer : IComposer
{
    [Id(0)]
    public required int RequesterRoomId { get; init; }
}
