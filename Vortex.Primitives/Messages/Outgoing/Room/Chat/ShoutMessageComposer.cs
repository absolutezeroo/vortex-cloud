using Orleans;

namespace Vortex.Primitives.Messages.Outgoing.Room.Chat;

[GenerateSerializer, Immutable]
public sealed record ShoutMessageComposer : ChatMessageComposer;
