using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Chat;

[GenerateSerializer, Immutable]
public sealed record RoomChatSettingsMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
