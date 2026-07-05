using Orleans;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Moderation;

[GenerateSerializer, Immutable]
public sealed record RoomChatlogEventMessageComposer : IComposer
{
    [Id(0)]
    public required ChatlogBlockSnapshot Block { get; init; }
}
