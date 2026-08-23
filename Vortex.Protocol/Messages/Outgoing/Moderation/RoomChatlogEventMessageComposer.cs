using Orleans;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Moderation;

[GenerateSerializer, Immutable]
public sealed record RoomChatlogEventMessageComposer : IComposer
{
    [Id(0)]
    public required ChatlogBlockSnapshot Block { get; init; }
}
