using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Chat;

[GenerateSerializer, Immutable]
public sealed record UserTypingMessageComposer : IComposer
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required bool IsTyping { get; init; }
}
