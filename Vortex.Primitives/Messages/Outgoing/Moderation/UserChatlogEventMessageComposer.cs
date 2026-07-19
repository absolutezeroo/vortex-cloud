using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Moderation;

[GenerateSerializer, Immutable]
public sealed record UserChatlogEventMessageComposer : IComposer
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required string UserName { get; init; }

    [Id(2)]
    public required ImmutableArray<ChatlogBlockSnapshot> Rooms { get; init; }
}
