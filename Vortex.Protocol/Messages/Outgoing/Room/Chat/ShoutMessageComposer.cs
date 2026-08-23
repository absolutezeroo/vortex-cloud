using Orleans;

namespace Vortex.Protocol.Messages.Outgoing.Room.Chat;

[GenerateSerializer, Immutable]
public sealed record ShoutMessageComposer : ChatMessageComposer;
