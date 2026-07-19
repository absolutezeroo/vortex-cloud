using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Chat;

[GenerateSerializer, Immutable]
public sealed record RoomFilterSettingsMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
